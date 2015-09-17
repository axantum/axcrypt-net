﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Axantum.AxCrypt.Api.Model
{
    [JsonObject(MemberSerialization.OptIn)]
    public class KeyPairResponse : CommonResponse
    {
        public KeyPairResponse()
        {
            KeyPair = new KeyPair();
        }

        [JsonProperty("keypair")]
        public KeyPair KeyPair { get; private set; }
    }
}