﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2013, Svante Seleborg, All Rights Reserved
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
using Axantum.AxCrypt.Core.Session;
using NUnit.Framework;
using System;
using System.Linq;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestPassphraseIdentity
    {
        [SetUp]
        public static void Setup()
        {
            SetupAssembly.AssemblySetup();
        }

        [TearDown]
        public static void Teardown()
        {
            SetupAssembly.AssemblyTeardown();
        }

        [Test]
        public static void TestEmptyField()
        {
            PassphraseIdentity zero = new PassphraseIdentity(String.Empty, AesKey.Zero);
            PassphraseIdentity nonzero = new PassphraseIdentity("id", new AesKey());

            Assert.That(zero.Key, Is.EqualTo(PassphraseIdentity.Empty.Key));
            Assert.That(zero.Name, Is.EqualTo(PassphraseIdentity.Empty.Name));
            Assert.That(zero.Thumbprint, Is.EqualTo(PassphraseIdentity.Empty.Thumbprint));

            Assert.That(nonzero.Key, Is.Not.EqualTo(PassphraseIdentity.Empty.Key));
            Assert.That(nonzero.Name, Is.Not.EqualTo(PassphraseIdentity.Empty.Name));
            Assert.That(nonzero.Thumbprint, Is.Not.EqualTo(PassphraseIdentity.Empty.Thumbprint));
        }
    }
}