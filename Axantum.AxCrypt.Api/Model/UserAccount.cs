﻿using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axantum.AxCrypt.Api.Model
{
    /// <summary>
    /// A summary of information we know about a user, typically fetched at the start of a session.
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class UserAccount
    {
        public UserAccount(string userName, SubscriptionLevel level, DateTime expiration, AccountStatus status, IEnumerable<AccountKey> keys)
        {
            UserName = userName;
            SubscriptionLevel = level;
            LevelExpiration = expiration;
            AccountKeys = keys.ToList();
            AccountStatus = status;
        }

        public UserAccount(string userName, SubscriptionLevel level, DateTime expiration, AccountStatus status)
            : this(userName, level, expiration, status, new AccountKey[0])
        {
        }

        public UserAccount(string userName, SubscriptionLevel level, AccountStatus status)
            : this(userName, level, DateTime.MinValue, status, new AccountKey[0])
        {
        }

        public UserAccount(string userName)
            : this(userName, SubscriptionLevel.Unknown, AccountStatus.Unknown)
        {
        }

        public UserAccount()
            : this(String.Empty)
        {
        }

        /// <summary>
        /// Gets the account keys.
        /// </summary>
        /// <value>
        /// The account keys.
        /// </value>
        [JsonProperty("keys")]
        public IList<AccountKey> AccountKeys { get; private set; }

        /// <summary>
        /// Gets the email address of the user.
        /// </summary>
        /// <value>
        /// The email address.
        /// </value>
        [JsonProperty("user")]
        public string UserName { get; private set; }

        /// <summary>
        /// Gets the currently valid subscription level.
        /// </summary>
        /// <value>
        /// The subscription level. Valid values are "" (unknown), "Free" and "Premium".
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("level")]
        public SubscriptionLevel SubscriptionLevel { get; private set; }

        /// <summary>
        /// Gets the level expiration date and time UTC.
        /// </summary>
        /// <value>
        /// The level expiration date and time UTC.
        /// </value>
        [JsonProperty("expiration")]
        public DateTime LevelExpiration { get; private set; }

        /// <summary>
        /// Gets the account status.
        /// </summary>
        /// <value>
        /// The status (Unknown, Unverified, Verified).
        /// </value>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Justification = "Serialization")]
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty("status")]
        public AccountStatus AccountStatus { get; private set; }
    }
}