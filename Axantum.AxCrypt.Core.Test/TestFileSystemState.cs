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
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Session;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Axantum.AxCrypt.Core.Test
{
    [TestFixture]
    public static class TestFileSystemState
    {
        private static readonly string _rootPath = Path.GetPathRoot(Environment.CurrentDirectory);
        private static readonly string _encryptedAxxPath = Path.Combine(_rootPath, "Encrypted-txt.axx");
        private static readonly string _encrypted1AxxPath = Path.Combine(_rootPath, "Encrypted1-txt.axx");
        private static readonly string _encrypted2AxxPath = Path.Combine(_rootPath, "Encrypted2-txt.axx");
        private static readonly string _encrypted3AxxPath = Path.Combine(_rootPath, "Encrypted3-txt.axx");
        private static readonly string _encrypted4AxxPath = Path.Combine(_rootPath, "Encrypted4-txt.axx");
        private static readonly string _decryptedTxtPath = Path.Combine(_rootPath, "Decrypted.txt");
        private static readonly string _decrypted1TxtPath = Path.Combine(_rootPath, "Decrypted1.txt");
        private static readonly string _decrypted2TxtPath = Path.Combine(_rootPath, "Decrypted2.txt");
        private static readonly string _decrypted3TxtPath = Path.Combine(_rootPath, "Decrypted3.txt");
        private static readonly string _decrypted4TxtPath = Path.Combine(_rootPath, "Decrypted4.txt");

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
        public static void TestLoadNew()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(state, Is.Not.Null, "An instance should always be instantiated.");
                Assert.That(state.ActiveFiles.Count(), Is.EqualTo(0), "A new state should not have any active files.");
            }
        }

        [Test]
        public static void TestLoadExisting()
        {
            ActiveFile activeFile;
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(state, Is.Not.Null, "An instance should always be instantiated.");
                Assert.That(state.ActiveFiles.Count(), Is.EqualTo(0), "A new state should not have any active files.");

                activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.AssumedOpenAndDecrypted, true);
                state.Add(activeFile);
                state.Save();
            }

            using (FileSystemState reloadedState = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(reloadedState, Is.Not.Null, "An instance should always be instantiated.");
                Assert.That(reloadedState.ActiveFiles.Count(), Is.EqualTo(1), "The reloaded state should have one active file.");
                Assert.That(reloadedState.ActiveFiles.First().ThumbprintMatch(activeFile.Key), Is.True, "The reloaded thumbprint should  match the key.");
            }
        }

        [Test]
        public static void TestActiveFileChangedEvent()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                bool wasHere;
                state.ActiveFileChanged += new EventHandler<ActiveFileChangedEventArgs>((object sender, ActiveFileChangedEventArgs e) => { wasHere = true; });
                ActiveFile activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("a"), ActiveFileStatus.AssumedOpenAndDecrypted, true);

                wasHere = false;
                state.Add(activeFile);
                Assert.That(state.ActiveFiles.Count(), Is.EqualTo(1), "After the Add() the state should have one active file.");
                Assert.That(wasHere, Is.True, "After the Add(), the changed event should have been raised.");

                wasHere = false;
                state.RemoveActiveFile(activeFile);
                Assert.That(wasHere, Is.True, "After the Remove(), the changed event should have been raised.");
                Assert.That(state.ActiveFiles.Count(), Is.EqualTo(0), "After the Remove() the state should have no active files.");
            }
        }

        [Test]
        public static void TestStatusMaskAtLoad()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                ActiveFile activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable, true);
                state.Add(activeFile);
                state.Save();

                FileSystemState reloadedState = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt"));
                Assert.That(reloadedState, Is.Not.Null, "An instance should always be instantiated.");
                Assert.That(reloadedState.ActiveFiles.Count(), Is.EqualTo(1), "The reloaded state should have one active file.");
                Assert.That(reloadedState.ActiveFiles.First().Status, Is.EqualTo(ActiveFileStatus.AssumedOpenAndDecrypted), "When reloading saved state, some statuses should be masked away.");
            }
        }

        [Test]
        public static void TestFindEncryptedPath()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                ActiveFile activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable, true);
                state.Add(activeFile);

                ActiveFile byEncryptedPath = state.FindActiveFileFromEncryptedPath(_encryptedAxxPath);
                Assert.That(byEncryptedPath.EncryptedFileInfo.FullName, Is.EqualTo(_encryptedAxxPath), "The search should return the same path.");

                ActiveFile notFoundEncrypted = state.FindActiveFileFromEncryptedPath(Path.Combine(_rootPath, "notfoundfile.txt"));
                Assert.That(notFoundEncrypted, Is.Null, "A search that does not succeed should return null.");
            }
        }

        [Test]
        public static void TestForEach()
        {
            bool changedEventWasRaised = false;
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                state.ActiveFileChanged += ((object sender, ActiveFileChangedEventArgs e) =>
                {
                    changedEventWasRaised = true;
                });

                ActiveFile activeFile;
                activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted1AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted1TxtPath), new GenericPassphrase("passphrase1"), ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable, true);
                state.Add(activeFile);
                activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted2AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted2TxtPath), new GenericPassphrase("passphrase2"), ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable, true);
                state.Add(activeFile);
                activeFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted3AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted3TxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable, true);
                state.Add(activeFile);
                Assert.That(changedEventWasRaised, Is.True, "The change event should have been raised by the adding of active files.");

                changedEventWasRaised = false;
                Assert.That(state.ActiveFiles.Count(), Is.EqualTo(3), "There should be three.");
                int i = 0;
                state.ForEach(ChangedEventMode.RaiseOnlyOnModified, (ActiveFile activeFileArgument) =>
                {
                    ++i;
                    return activeFileArgument;
                });
                Assert.That(i, Is.EqualTo(3), "The iteration should have visited three active files.");
                Assert.That(changedEventWasRaised, Is.False, "No change event should have been raised.");

                i = 0;
                state.ForEach(ChangedEventMode.RaiseAlways, (ActiveFile activeFileArgument) =>
                {
                    ++i;
                    return activeFileArgument;
                });
                Assert.That(i, Is.EqualTo(3), "The iteration should have visited three active files.");
                Assert.That(changedEventWasRaised, Is.True, "The change event should have been raised.");

                changedEventWasRaised = false;
                i = 0;
                state.ForEach(ChangedEventMode.RaiseAlways, (ActiveFile activeFileArgument) =>
                {
                    ++i;
                    return new ActiveFile(activeFileArgument, activeFile.Status | ActiveFileStatus.Error);
                });
                Assert.That(i, Is.EqualTo(3), "The iteration should have visited three active files.");
                Assert.That(changedEventWasRaised, Is.True, "The change event should have been raised.");
            }
        }

        [Test]
        public static void TestDecryptedActiveFiles()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                ActiveFile decryptedFile1 = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase1"), ActiveFileStatus.AssumedOpenAndDecrypted, true);
                state.Add(decryptedFile1);

                ActiveFile decryptedFile2 = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted2AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted2TxtPath), new GenericPassphrase("passphrase2"), ActiveFileStatus.DecryptedIsPendingDelete, true);
                state.Add(decryptedFile2);

                ActiveFile notDecryptedFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted3AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted3TxtPath), new GenericPassphrase("passphrase3"), ActiveFileStatus.NotDecrypted, true);
                state.Add(notDecryptedFile);

                ActiveFile errorFile = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encrypted4AxxPath), Factory.New<IRuntimeFileInfo>(_decrypted4TxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.Error, true);
                state.Add(errorFile);

                IList<ActiveFile> decryptedFiles = state.DecryptedActiveFiles;
                Assert.That(decryptedFiles.Count, Is.EqualTo(2), "There should be two decrypted files.");
                Assert.That(decryptedFiles.Contains(decryptedFile1), "A file marked as AssumedOpenAndDecrypted should be found.");
                Assert.That(decryptedFiles.Contains(decryptedFile2), "A file marked as DecryptedIsPendingDelete should be found.");
                Assert.That(decryptedFiles.Contains(notDecryptedFile), Is.Not.True, "A file marked as NotDecrypted should not be found.");
            }
        }

        [Test]
        public static void TestDoubleDispose()
        {
            FileSystemState state = new FileSystemState();
            state.Dispose();

            Assert.DoesNotThrow(() => { state.Dispose(); });
        }

        [Test]
        public static void TestArgumentNull()
        {
            using (FileSystemState state = new FileSystemState())
            {
                ActiveFile nullActiveFile = null;
                string nullPath = null;
                Func<ActiveFile, ActiveFile> nullAction = null;
                IRuntimeFileInfo nullFileInfo = null;

                Assert.Throws<ArgumentNullException>(() => { state.RemoveActiveFile(nullActiveFile); });
                Assert.Throws<ArgumentNullException>(() => { state.Add(nullActiveFile); });
                Assert.Throws<ArgumentNullException>(() => { state.FindActiveFileFromEncryptedPath(nullPath); });
                Assert.Throws<ArgumentNullException>(() => { state.ForEach(ChangedEventMode.RaiseAlways, nullAction); });
                Assert.Throws<ArgumentNullException>(() => { FileSystemState.Create(nullFileInfo); });
            }
        }

        [Test]
        public static void TestInvalidXml()
        {
            string badXml = @"<FileSystemState xmlns=""http://www.axantum.com/Serialization/"" xmlns:i=""http://www.w3.org/2001/XMLSchema-instance"">";

            IRuntimeFileInfo stateInfo = Instance.WorkFolder.FileInfo.Combine("mystate.txt");
            using (Stream stream = stateInfo.OpenWrite())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(badXml);
                stream.Write(bytes, 0, bytes.Length);
            }

            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(state.ActiveFileCount, Is.EqualTo(0), "After loading damaged state, the count should be zero.");

                ActiveFile decryptedFile1 = new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.AssumedOpenAndDecrypted, true);
                state.Add(decryptedFile1);

                Assert.That(state.ActiveFileCount, Is.EqualTo(1), "After adding a file, the count should be one.");
            }
        }

        [Test]
        public static void TestWatchedFolders()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(state.WatchedFolders, Is.Not.Null, "There should be a Watched Folders instance.");
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(0), "There should be no Watched folders.");

                FakeRuntimeFileInfo.AddFolder(_rootPath);
                state.AddWatchedFolder(new WatchedFolder(_rootPath, SymmetricKeyThumbprint.Zero));
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(1), "There should be one Watched Folder.");

                state.AddWatchedFolder(new WatchedFolder(_rootPath, SymmetricKeyThumbprint.Zero));
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(1), "There should still only be one Watched Folder.");

                state.Save();
            }

            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(1), "There should be one Watched Folder.");

                Assert.That(state.WatchedFolders.First(), Is.EqualTo(new WatchedFolder(_rootPath, SymmetricKeyThumbprint.Zero)), "The Watched Folder should be equal to this.");

                state.RemoveWatchedFolder(Instance.WorkFolder.FileInfo.Combine("mystate.txt"));
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(1), "There should still be one Watched folders.");

                state.RemoveWatchedFolder(Factory.New<IRuntimeFileInfo>(_rootPath));
                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(0), "There should be no Watched folders now.");
            }
        }

        [Test]
        public static void TestWatchedFolderChanged()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                FakeRuntimeFileInfo.AddFolder(_rootPath);
                state.AddWatchedFolder(new WatchedFolder(_rootPath, SymmetricKeyThumbprint.Zero));

                Assert.That(state.ActiveFileCount, Is.EqualTo(0));

                FakeRuntimeFileInfo.AddFile(_encryptedAxxPath, null);
                Assert.That(state.ActiveFileCount, Is.EqualTo(0));

                state.Add(new ActiveFile(Factory.New<IRuntimeFileInfo>(_encryptedAxxPath), Factory.New<IRuntimeFileInfo>(_decryptedTxtPath), new GenericPassphrase("passphrase"), ActiveFileStatus.NotDecrypted, true));
                Assert.That(state.ActiveFileCount, Is.EqualTo(1));

                Factory.New<IRuntimeFileInfo>(_encryptedAxxPath).Delete();
                Assert.That(state.ActiveFileCount, Is.EqualTo(0), "When deleted, the active file count should be zero again.");
            }
        }

        [Test]
        public static void TestWatchedFolderRemoved()
        {
            using (FileSystemState state = FileSystemState.Create(Instance.WorkFolder.FileInfo.Combine("mystate.txt")))
            {
                FakeRuntimeFileInfo.AddFolder(_rootPath);
                state.AddWatchedFolder(new WatchedFolder(_rootPath, SymmetricKeyThumbprint.Zero));

                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(1));

                FakeRuntimeFileInfo.RemoveFileOrFolder(_rootPath);

                Assert.That(state.WatchedFolders.Count(), Is.EqualTo(0));
            }
        }

        [Test]
        public static void TestChangedEvent()
        {
            bool wasHere = false;

            SessionNotify notificationMonitor = new SessionNotify();

            notificationMonitor.Notification += (object sender, SessionNotificationEventArgs e) => { wasHere = e.Notification.NotificationType == SessionNotificationType.ActiveFileChange; };
            notificationMonitor.Notify(new SessionNotification(SessionNotificationType.ActiveFileChange));

            Assert.That(wasHere, Is.True, "The RaiseChanged() method should raise the event immediately.");
        }
    }
}