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

using Axantum.AxCrypt.Core.Session;
using NUnit.Framework;
using System;
using System.Linq;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestFactoryRegistry
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
        public static void TestRegisterParameterless()
        {
            bool wasCalled = false;
            TypeMap.Register.New<int>(() => { wasCalled = true; return 13; });

            int value = TypeMap.Resolve.New<int>();
            Assert.That(value, Is.EqualTo(13));
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public static void TestRegisterWithParameter()
        {
            bool wasCalled = false;
            TypeMap.Register.New<int, int>((argument) => { wasCalled = true; return argument; });

            int value = TypeMap.Resolve.New<int, int>(27);
            Assert.That(value, Is.EqualTo(27));
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public static void TestDefault()
        {
            AxCryptFile axCryptFile = TypeMap.Resolve.New<AxCryptFile>();
            Assert.That(axCryptFile is AxCryptFile, Is.True);

            TypeMap.Register.New<FileSystemState>(() => new FileSystemState());
            ActiveFileAction actions = TypeMap.Resolve.New<ActiveFileAction>();
            Assert.That(actions is ActiveFileAction, Is.True);
        }

        [Test]
        public static void TestNotRegistered()
        {
            Assert.Throws<ArgumentException>(() => TypeMap.Resolve.New<int>());
            Assert.Throws<ArgumentException>(() => TypeMap.Resolve.New<int, int>(13));
        }

        [Test]
        public static void TestNotRegisteredSingleton()
        {
            Assert.Throws<ArgumentException>(() => TypeMap.Resolve.Singleton<object>());
        }

        private class MyDisposable : IDisposable
        {
            public bool IsDisposed { get; set; }

            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Test]
        public static void TestClearDisposable()
        {
            TypeMap.Register.Singleton(() => new MyDisposable());

            MyDisposable md = TypeMap.Resolve.Singleton<MyDisposable>();
            Assert.That(md.IsDisposed, Is.False);

            TypeMap.Register.Clear();
            Assert.That(md.IsDisposed, Is.True);
        }

        [Test]
        public static void TestSetDisposableSingletonTwice()
        {
            TypeMap.Register.Singleton(() => new MyDisposable());

            MyDisposable md = TypeMap.Resolve.Singleton<MyDisposable>();
            Assert.That(md.IsDisposed, Is.False);

            TypeMap.Register.Singleton(() => new MyDisposable());
            Assert.That(md.IsDisposed, Is.True);
        }
    }
}