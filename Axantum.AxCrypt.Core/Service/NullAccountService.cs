﻿using Axantum.AxCrypt.Abstractions.Rest;
using Axantum.AxCrypt.Api.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axantum.AxCrypt.Core.Service
{
    public class NullAccountService : IAccountService
    {
        public NullAccountService(RestIdentity identity)
        {
            Identity = identity;
        }

        public bool HasAccounts
        {
            get
            {
                return false;
            }
        }

        public RestIdentity Identity
        {
            get; private set;
        }

        public SubscriptionLevel Level
        {
            get
            {
                return SubscriptionLevel.Unknown;
            }
        }

        public AccountStatus Status
        {
            get
            {
                return AccountStatus.Unknown;
            }
        }

        public bool ChangePassphrase(string passphrase)
        {
            Identity = new RestIdentity(Identity.User, passphrase);
            return true;
        }

        public IList<UserKeyPair> List()
        {
            return new UserKeyPair[0];
        }

        public void Save(IEnumerable<UserKeyPair> keyPairs)
        {
        }
    }
}