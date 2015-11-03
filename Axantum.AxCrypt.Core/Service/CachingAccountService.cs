﻿using Axantum.AxCrypt.Abstractions;
using Axantum.AxCrypt.Api.Model;
using Axantum.AxCrypt.Common;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Axantum.AxCrypt.Abstractions.TypeResolve;

namespace Axantum.AxCrypt.Core.Service
{
    public class CachingAccountService : IAccountService
    {
        private static readonly TimeSpan ItemExpiration = TimeSpan.Zero;

        private class CacheKey : ICacheKey
        {
            public static CacheKey RootKey = new CacheKey();

            private string _key;

            private CacheKey()
                : this("RootKey", null)
            {
            }

            public CacheKey(string key)
                : this(key, RootKey)
            {
            }

            public CacheKey(string key, ICacheKey parentCacheKey)
            {
                _key = key;
                ParentCacheKey = parentCacheKey;
            }

            public CacheKey SubKey(string key)
            {
                CacheKey subKey = new CacheKey(key, this);
                return subKey;
            }

            public ICacheKey ParentCacheKey { get; }

            public string Key
            {
                get
                {
                    if (ParentCacheKey == null)
                    {
                        return _key;
                    }
                    return ParentCacheKey.Key + "-" + _key;
                }
            }

            public TimeSpan Expiration { get { return ItemExpiration; } }
        }

        private IAccountService _service;

        private CacheKey _key;

        public CachingAccountService(IAccountService service)
        {
            _service = service;
            _key = CacheKey.RootKey.SubKey(nameof(CachingAccountService)).SubKey(service.Identity.UserEmail.Address);
        }

        public bool HasAccounts
        {
            get
            {
                return New<ICache>().Get(_key.SubKey(nameof(HasAccounts)), () => _service.HasAccounts);
            }
        }

        public LogOnIdentity Identity
        {
            get
            {
                return _service.Identity;
            }
        }

        public SubscriptionLevel Level
        {
            get
            {
                return New<ICache>().Get(_key.SubKey(nameof(Level)), () => _service.Level);
            }
        }

        public bool ChangePassphrase(Passphrase passphrase)
        {
            bool result = false;
            New<ICache>().Update(() => result = _service.ChangePassphrase(passphrase), _key);
            return result;
        }

        public async Task<IList<UserKeyPair>> ListAsync()
        {
            return await New<ICache>().GetAsync(_key.SubKey(nameof(ListAsync)), () => _service.ListAsync()).Free();
        }

        public async Task<UserKeyPair> CurrentKeyPairAsync()
        {
            return await New<ICache>().GetAsync(_key.SubKey(nameof(CurrentKeyPairAsync)), () => _service.CurrentKeyPairAsync()).Free();
        }

        public async Task PasswordResetAsync(string verificationCode)
        {
            await _service.PasswordResetAsync(verificationCode).Free();
        }

        public async Task SaveAsync(IEnumerable<UserKeyPair> keyPairs)
        {
            await New<ICache>().UpdateAsync(() => _service.SaveAsync(keyPairs), _key).Free();
        }

        public async Task SignupAsync(string emailAddress)
        {
            await New<ICache>().UpdateAsync(() => _service.SignupAsync(emailAddress), _key);
        }

        public async Task<AccountStatus> StatusAsync()
        {
            AccountStatus status = await New<ICache>().GetAsync(_key.SubKey(nameof(StatusAsync)), () => _service.StatusAsync()).Free();
            if (status == AccountStatus.Offline)
            {
                New<ICache>().Remove(_key);
            }
            return status;
        }

        public async Task<UserPublicKey> PublicKeyAsync()
        {
            return await New<ICache>().GetAsync(_key.SubKey(nameof(PublicKeyAsync)), () => _service.PublicKeyAsync()).Free();
        }
    }
}