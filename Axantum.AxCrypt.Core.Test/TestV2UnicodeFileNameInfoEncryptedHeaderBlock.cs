﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2014, Svante Seleborg, All Rights Reserved
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

using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Header;
using Axantum.AxCrypt.Core.Portable;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Mono.Portable;
using NUnit.Framework;
using System;
using System.Linq;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestV2UnicodeFileNameInfoEncryptedHeaderBlock
    {
        [SetUp]
        public static void Setup()
        {
            TypeMap.Register.Singleton<IPortableFactory>(() => new PortableFactory());
            TypeMap.Register.Singleton<CryptoFactory>(() => CreateCryptoFactory());
            TypeMap.Register.Singleton<ICryptoPolicy>(() => new ProCryptoPolicy());
        }

        private static CryptoFactory CreateCryptoFactory()
        {
            CryptoFactory factory = new CryptoFactory();
            factory.Add(() => new V2Aes256CryptoFactory());
            factory.Add(() => new V1Aes128CryptoFactory());

            return factory;
        }

        [TearDown]
        public static void Teardown()
        {
            TypeMap.Register.Clear();
        }

        [Test]
        public static void TestClone()
        {
            TypeMap.Register.Singleton<IRandomGenerator>(() => new FakeRandomGenerator());
            TypeMap.Register.Singleton<IRuntimeEnvironment>(() => new FakeRuntimeEnvironment());

            V2UnicodeFileNameInfoEncryptedHeaderBlock headerBlock = new V2UnicodeFileNameInfoEncryptedHeaderBlock(new V2AesCrypto(SymmetricKey.Zero256, SymmetricIV.Zero128, 0));
            headerBlock.FileName = "A file name";
            Assert.That(headerBlock.FileName, Is.EqualTo("A file name"));

            V2UnicodeFileNameInfoEncryptedHeaderBlock clone = (V2UnicodeFileNameInfoEncryptedHeaderBlock)headerBlock.Clone();
            Assert.That(clone.FileName, Is.EqualTo("A file name"));
        }

        [Test]
        public static void TestBadDataCausedByBadKeyForExample()
        {
            TypeMap.Register.Singleton<IRandomGenerator>(() => new FakeRandomGenerator());
            TypeMap.Register.Singleton<IRuntimeEnvironment>(() => new FakeRuntimeEnvironment());

            V2UnicodeFileNameInfoEncryptedHeaderBlock headerBlock = new V2UnicodeFileNameInfoEncryptedHeaderBlock(new V2AesCrypto(SymmetricKey.Zero256, SymmetricIV.Zero128, 0));
            headerBlock.FileName = "A file name";
            Assert.That(headerBlock.FileName, Is.EqualTo("A file name"));

            headerBlock.HeaderCrypto = new V2AesCrypto(new V2DerivedKey(new Passphrase("passphrase"), 256).DerivedKey, SymmetricIV.Zero128, 0);
            string s;
            Assert.Throws<InvalidOperationException>(() => s = headerBlock.FileName);
        }
    }
}