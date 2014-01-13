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

using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Session;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestWorkFolderWatcher
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
        public static void TestWorkFolderWatcherSimple()
        {
            Factory.Instance.Singleton<SessionNotify>(() => new Mock<SessionNotify>().Object);

            using (WorkFolderWatcher wfw = new WorkFolderWatcher())
            {
                string fileName = Instance.WorkFolder.FileInfo.Combine("New File.txt").FullName;
                FakeRuntimeFileInfo.AddFile(fileName, null);

                Mock.Get(Instance.SessionNotify).Verify(s => s.Notify(It.Is<SessionNotification>(n => n.NotificationType == SessionNotificationType.WorkFolderChange && n.FullName == fileName)), Times.Once);

                Factory.New<IRuntimeFileInfo>(fileName).Delete();
                Mock.Get(Instance.SessionNotify).Verify(s => s.Notify(It.Is<SessionNotification>(n => n.NotificationType == SessionNotificationType.WorkFolderChange && n.FullName == fileName)), Times.Exactly(2));

                IRuntimeFileInfo fileSystemStateInfo = Instance.WorkFolder.FileInfo.Combine("FileSystemState.xml");
                fileSystemStateInfo.Delete();
                Mock.Get(Instance.SessionNotify).Verify(s => s.Notify(It.Is<SessionNotification>(n => n.NotificationType == SessionNotificationType.WorkFolderChange && n.FullName == fileSystemStateInfo.FullName)), Times.Never);
            }
        }
    }
}