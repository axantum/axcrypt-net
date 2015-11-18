﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2015, Svante Seleborg, All Rights Reserved
 *
 * This file is part of AxCrypt.
 *
 * AxCrypt is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * AxCrypt is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with AxCrypt.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at http://bitbucket.org/axantum/axcrypt-net please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axantum.com for more information about the author.
*/

#endregion Coypright and License

using Axantum.AxCrypt.Api.Model;
using Axantum.AxCrypt.Common;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Service
{
    public class LocalAccountService : IAccountService
    {
        private static readonly Task _completedTask = Task.FromResult(true);

        private static Regex _userKeyPairFilePattern = new Regex(@"^Keys-([\d]+)-txt\.axx$");

        private IDataContainer _workContainer;

        public LocalAccountService(LogOnIdentity identity, IDataContainer workContainer)
        {
            if (identity == null)
            {
                throw new ArgumentNullException(nameof(identity));
            }
            if (workContainer == null)
            {
                throw new ArgumentNullException(nameof(workContainer));
            }

            Identity = identity;
            _workContainer = workContainer;
        }

        public Task<SubscriptionLevel> LevelAsync()
        {
            if (Identity == LogOnIdentity.Empty)
            {
                return Task.FromResult(SubscriptionLevel.Unknown);
            }
            return Task.FromResult(LoadUserAccount().SubscriptionLevel);
        }

        public async Task<AccountStatus> StatusAsync(EmailAddress email)
        {
            return await Task.Run(() =>
            {
                if (LoadUserAccounts().Accounts.Any(a => EmailAddress.Parse(a.UserName) == email) || UserKeyPairFiles().Any())
                {
                    return AccountStatus.Verified;
                }
                return AccountStatus.NotFound;
            }).Free();
        }

        public bool HasAccounts
        {
            get
            {
                if (Identity.UserEmail == EmailAddress.Empty)
                {
                    throw new InvalidOperationException("The account service requires a user.");
                }
                if (LoadUserAccounts().Accounts.Any())
                {
                    return true;
                }

                if (UserKeyPairFiles().Any())
                {
                    return true;
                }

                return false;
            }
        }

        public Task<UserAccount> AccountAsync()
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }
            UserAccount userAccount = LoadUserAccount();
            return Task.FromResult(userAccount);
        }

        public Task<IList<UserKeyPair>> ListAsync()
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            return Task.FromResult(TryLoadUserKeyPairs());
        }

        public Task<UserKeyPair> CurrentKeyPairAsync()
        {
            throw new NotImplementedException();
        }

        public async Task SaveAsync(UserAccount account)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            await Task.Run(() =>
            {
                UserAccounts userAccounts = LoadUserAccounts();
                UserAccount userAccount = userAccounts.Accounts.FirstOrDefault(ua => EmailAddress.Parse(ua.UserName) == Identity.UserEmail);
                if (userAccount == null)
                {
                    userAccount = new UserAccount(Identity.UserEmail.Address, SubscriptionLevel.Unknown, DateTime.MinValue, AccountStatus.Unknown, new AccountKey[0]);
                    userAccounts.Accounts.Add(userAccount);
                }

                IEnumerable<AccountKey> accountKeysToUpdate = account.AccountKeys;
                IEnumerable<AccountKey> existingAccountKeys = userAccount.AccountKeys;
                IEnumerable<AccountKey> accountKeys = userAccount.AccountKeys.Union(accountKeysToUpdate);

                if (accountKeys.Count() == existingAccountKeys.Count() && userAccount.AccountStatus == account.AccountStatus && userAccount.SubscriptionLevel == userAccount.SubscriptionLevel && userAccount.LevelExpiration == account.LevelExpiration)
                {
                    return;
                }

                userAccount = new UserAccount(userAccount.UserName, account.SubscriptionLevel, account.LevelExpiration, account.AccountStatus);
                SaveInternal(userAccounts, userAccount, accountKeys);
            }).Free();
        }

        public async Task SaveAsync(IEnumerable<UserKeyPair> keyPairs)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requies a user.");
            }

            await Task.Run(() =>
            {
                UserAccounts userAccounts = LoadUserAccounts();
                UserAccount userAccount = userAccounts.Accounts.FirstOrDefault(ua => EmailAddress.Parse(ua.UserName) == Identity.UserEmail);
                if (userAccount == null)
                {
                    userAccount = new UserAccount(Identity.UserEmail.Address, SubscriptionLevel.Unknown, DateTime.MinValue, AccountStatus.Unknown, new AccountKey[0]);
                    userAccounts.Accounts.Add(userAccount);
                }

                IEnumerable<AccountKey> accountKeysToUpdate = keyPairs.Select(uk => uk.ToAccountKey(Identity.Passphrase));
                IEnumerable<AccountKey> existingAccountKeys = userAccount.AccountKeys;
                IEnumerable<AccountKey> accountKeys = userAccount.AccountKeys.Union(accountKeysToUpdate);

                if (accountKeys.Count() == existingAccountKeys.Count())
                {
                    return;
                }

                SaveInternal(userAccounts, userAccount, accountKeys);
            }).Free();
        }

        private void SaveInternal(UserAccounts userAccounts, UserAccount userAccount, IEnumerable<AccountKey> accountKeys)
        {
            UserAccounts userAccountsToSave = new UserAccounts();
            foreach (UserAccount ua in userAccounts.Accounts.Where(a => a.UserName != userAccount.UserName))
            {
                userAccountsToSave.Accounts.Add(ua);
            }
            userAccount.AccountKeys.Clear();
            foreach (AccountKey ak in accountKeys)
            {
                userAccount.AccountKeys.Add(ak);
            }
            userAccountsToSave.Accounts.Add(userAccount);

            using (StreamWriter writer = new StreamWriter(_workContainer.FileItemInfo("UserAccounts.txt").OpenWrite()))
            {
                userAccountsToSave.SerializeTo(writer);
            }
        }

        public bool ChangePassphrase(Passphrase passphrase)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            SaveAsync(ListAsync().Result).Wait();
            return true;
        }

        private IList<UserKeyPair> TryLoadUserKeyPairs()
        {
            IEnumerable<AccountKey> userAccountKeys = LoadUserAccount().AccountKeys;
            List<UserKeyPair> userKeys = LoadValidUserKeysFromAccountKeys(userAccountKeys).ToList();
            if (!userKeys.Any())
            {
                IEnumerable<UserKeyPair> fromKeyPairFiles = UserKeyPair.Load(UserKeyPairFiles(), Identity.UserEmail, Identity.Passphrase);
                fromKeyPairFiles = userKeys.Where(uk => !userAccountKeys.Any(ak => new PublicKeyThumbprint(ak.Thumbprint) == uk.KeyPair.PublicKey.Thumbprint));
                userKeys.AddRange(fromKeyPairFiles);
            }

            return userKeys;
        }

        private IEnumerable<IDataStore> UserKeyPairFiles()
        {
            return _workContainer.Files.Where(f => _userKeyPairFilePattern.Match(f.Name).Success);
        }

        private IEnumerable<UserKeyPair> LoadValidUserKeysFromAccountKeys(IEnumerable<AccountKey> userAccountKeys)
        {
            return userAccountKeys.Select(ak => ak.ToUserKeyPair(Identity.Passphrase)).Where(ak => ak != null);
        }

        private UserAccount LoadUserAccount()
        {
            UserAccounts accounts = LoadUserAccounts();
            IEnumerable<UserAccount> users = accounts.Accounts.Where(ua => EmailAddress.Parse(ua.UserName) == Identity.UserEmail);
            if (!users.Any())
            {
                return new UserAccount(Identity.UserEmail.Address, SubscriptionLevel.Unknown, DateTime.MinValue, AccountStatus.Unknown, new AccountKey[0]);
            }

            return users.First();
        }

        private IDataStore UserAccountsStore
        {
            get
            {
                return _workContainer.FileItemInfo("UserAccounts.txt");
            }
        }

        public LogOnIdentity Identity
        {
            get;
        }

        public async Task SignupAsync(EmailAddress email)
        {
            await _completedTask;
        }

        private UserAccounts LoadUserAccounts()
        {
            if (!UserAccountsStore.IsAvailable)
            {
                return new UserAccounts();
            }

            using (StreamReader reader = new StreamReader(UserAccountsStore.OpenRead()))
            {
                return UserAccounts.DeserializeFrom(reader);
            }
        }

        public Task PasswordResetAsync(string verificationCode)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            return _completedTask;
        }

        public async Task<UserPublicKey> OtherPublicKeyAsync(EmailAddress email)
        {
            if (Identity.UserEmail == EmailAddress.Empty)
            {
                throw new InvalidOperationException("The account service requires a user.");
            }

            return await Task.Run(() =>
            {
                using (KnownPublicKeys knowPublicKeys = New<KnownPublicKeys>())
                {
                    UserPublicKey publicKey = knowPublicKeys.PublicKeys.Where(pk => pk.Email == email).FirstOrDefault();
                    return publicKey;
                }
            }).Free();
        }
    }
}