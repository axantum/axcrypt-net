﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2012, Svante Seleborg, All Rights Reserved
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
using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.Session;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Axantum.AxCrypt.Core.UI
{
    public class FileOperation
    {
        private FileSystemState _fileSystemState;
        private SessionNotificationMonitor _notificationMonitor;

        public FileOperation(FileSystemState fileSystemState, SessionNotificationMonitor notificationMonitor)
        {
            _fileSystemState = fileSystemState;
            _notificationMonitor = notificationMonitor;
        }

        public FileOperationStatus OpenAndLaunchApplication(string file, IEnumerable<AesKey> keys, IProgressContext progress)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            if (keys == null)
            {
                throw new ArgumentNullException("keys");
            }
            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            IRuntimeFileInfo fileInfo = OS.Current.FileInfo(file);
            if (!fileInfo.Exists)
            {
                if (Instance.Log.IsWarningEnabled)
                {
                    Instance.Log.LogWarning("Tried to open non-existing '{0}'.".InvariantFormat(fileInfo.FullName));
                }
                return FileOperationStatus.FileDoesNotExist;
            }

            ActiveFile destinationActiveFile = _fileSystemState.FindEncryptedPath(fileInfo.FullName);

            if (destinationActiveFile == null || !destinationActiveFile.DecryptedFileInfo.Exists)
            {
                IRuntimeFileInfo destinationFolderInfo = GetTemporaryDestinationFolder(destinationActiveFile);
                destinationActiveFile = TryDecrypt(fileInfo, destinationFolderInfo, keys, progress);
            }
            else
            {
                destinationActiveFile = CheckKeysForAlreadyDecryptedFile(destinationActiveFile, keys, progress);
            }

            if (destinationActiveFile == null)
            {
                return FileOperationStatus.InvalidKey;
            }

            _fileSystemState.Add(destinationActiveFile);
            _fileSystemState.Save();

            FileOperationStatus status = LaunchApplicationForDocument(destinationActiveFile);
            return status;
        }

        public FileOperationStatus OpenAndLaunchApplication(string file, AxCryptDocument document, IProgressContext progress)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            if (document == null)
            {
                throw new ArgumentNullException("document");
            }
            if (progress == null)
            {
                throw new ArgumentNullException("progress");
            }

            IRuntimeFileInfo fileInfo = OS.Current.FileInfo(file);

            ActiveFile destinationActiveFile = _fileSystemState.FindEncryptedPath(fileInfo.FullName);
            if (destinationActiveFile == null || !destinationActiveFile.DecryptedFileInfo.Exists)
            {
                IRuntimeFileInfo destinationFolderInfo = GetTemporaryDestinationFolder(destinationActiveFile);
                destinationActiveFile = DecryptActiveFileDocument(fileInfo, destinationFolderInfo, document, progress);
            }
            else
            {
                destinationActiveFile = new ActiveFile(destinationActiveFile, document.DocumentHeaders.KeyEncryptingKey);
            }

            _fileSystemState.Add(destinationActiveFile);
            _fileSystemState.Save();

            FileOperationStatus status = LaunchApplicationForDocument(destinationActiveFile);
            return status;
        }

        private FileOperationStatus LaunchApplicationForDocument(ActiveFile destinationActiveFile)
        {
            ILauncher process;
            try
            {
                if (Instance.Log.IsInfoEnabled)
                {
                    Instance.Log.LogInfo("Starting process for '{0}'".InvariantFormat(destinationActiveFile.DecryptedFileInfo.FullName));
                }
                process = OS.Current.Launch(destinationActiveFile.DecryptedFileInfo.FullName);
                if (process.WasStarted)
                {
                    process.Exited += new EventHandler(process_Exited);
                }
                else
                {
                    if (Instance.Log.IsInfoEnabled)
                    {
                        Instance.Log.LogInfo("Starting process for '{0}' did not start a process, assumed handled by the shell.".InvariantFormat(destinationActiveFile.DecryptedFileInfo.FullName));
                    }
                }
            }
            catch (Win32Exception w32ex)
            {
                if (Instance.Log.IsErrorEnabled)
                {
                    Instance.Log.LogError("Could not launch application for '{0}', Win32Exception was '{1}'.".InvariantFormat(destinationActiveFile.DecryptedFileInfo.FullName, w32ex.Message));
                }
                return FileOperationStatus.CannotStartApplication;
            }

            if (Instance.Log.IsWarningEnabled)
            {
                if (process.HasExited)
                {
                    Instance.Log.LogWarning("The process seems to exit immediately for '{0}'".InvariantFormat(destinationActiveFile.DecryptedFileInfo.FullName));
                }
            }

            if (Instance.Log.IsInfoEnabled)
            {
                Instance.Log.LogInfo("Launched and opened '{0}'.".InvariantFormat(destinationActiveFile.DecryptedFileInfo.FullName));
            }

            destinationActiveFile = new ActiveFile(destinationActiveFile, ActiveFileStatus.AssumedOpenAndDecrypted);
            _fileSystemState.Add(destinationActiveFile, process);
            _fileSystemState.Save();

            return FileOperationStatus.Success;
        }

        private void process_Exited(object sender, EventArgs e)
        {
            string path = ((ILauncher)sender).Path;
            if (Instance.Log.IsInfoEnabled)
            {
                Instance.Log.LogInfo("Process exit event for '{0}'.".InvariantFormat(path));
            }

            _notificationMonitor.Notify(new SessionNotification(SessionNotificationType.ProcessExit, path));
        }

        private static ActiveFile TryDecrypt(IRuntimeFileInfo sourceFileInfo, IRuntimeFileInfo destinationFolderInfo, IEnumerable<AesKey> keys, IProgressContext progress)
        {
            ActiveFile destinationActiveFile = null;
            foreach (AesKey key in keys)
            {
                if (Instance.Log.IsInfoEnabled)
                {
                    Instance.Log.LogInfo("Decrypting '{0}'".InvariantFormat(sourceFileInfo.FullName));
                }
                using (FileLock sourceLock = FileLock.Lock(sourceFileInfo))
                {
                    using (AxCryptDocument document = AxCryptFile.Document(sourceFileInfo, key, new ProgressContext()))
                    {
                        if (!document.PassphraseIsValid)
                        {
                            continue;
                        }

                        destinationActiveFile = DecryptActiveFileDocument(sourceFileInfo, destinationFolderInfo, document, progress);
                        break;
                    }
                }
            }
            return destinationActiveFile;
        }

        private static ActiveFile DecryptActiveFileDocument(IRuntimeFileInfo sourceFileInfo, IRuntimeFileInfo destinationFolderInfo, AxCryptDocument document, IProgressContext progress)
        {
            string destinationName = document.DocumentHeaders.FileName;
            string destinationPath = Path.Combine(destinationFolderInfo.FullName, destinationName);

            IRuntimeFileInfo destinationFileInfo = OS.Current.FileInfo(destinationPath);
            using (FileLock fileLock = FileLock.Lock(destinationFileInfo))
            {
                AxCryptFile.Decrypt(document, destinationFileInfo, AxCryptOptions.SetFileTimes, progress);
            }
            ActiveFile destinationActiveFile = new ActiveFile(sourceFileInfo, destinationFileInfo, document.DocumentHeaders.KeyEncryptingKey, ActiveFileStatus.AssumedOpenAndDecrypted | ActiveFileStatus.IgnoreChange);
            if (Instance.Log.IsInfoEnabled)
            {
                Instance.Log.LogInfo("File decrypted from '{0}' to '{1}'".InvariantFormat(sourceFileInfo.FullName, destinationActiveFile.DecryptedFileInfo.FullName));
            }
            return destinationActiveFile;
        }

        private static IRuntimeFileInfo GetTemporaryDestinationFolder(ActiveFile destinationActiveFile)
        {
            string destinationFolder;
            if (destinationActiveFile != null)
            {
                destinationFolder = Path.GetDirectoryName(destinationActiveFile.DecryptedFileInfo.FullName);
            }
            else
            {
                destinationFolder = Path.Combine(OS.Current.WorkFolder.FullName, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.DirectorySeparatorChar);
            }
            IRuntimeFileInfo destinationFolderInfo = OS.Current.FileInfo(destinationFolder);
            destinationFolderInfo.CreateFolder();
            return destinationFolderInfo;
        }

        public static string GetTemporaryDestinationName(string fileName)
        {
            string destinationFolder = Path.Combine(OS.Current.WorkFolder.FullName, Path.GetFileNameWithoutExtension(Path.GetRandomFileName()) + Path.DirectorySeparatorChar);
            return Path.Combine(destinationFolder, Path.GetFileName(fileName));
        }

        private static ActiveFile CheckKeysForAlreadyDecryptedFile(ActiveFile destinationActiveFile, IEnumerable<AesKey> keys, IProgressContext progress)
        {
            foreach (AesKey key in keys)
            {
                using (AxCryptDocument document = AxCryptFile.Document(destinationActiveFile.EncryptedFileInfo, key, progress))
                {
                    if (document.PassphraseIsValid)
                    {
                        if (Instance.Log.IsWarningEnabled)
                        {
                            Instance.Log.LogWarning("File was already decrypted and the key was known for '{0}' to '{1}'".InvariantFormat(destinationActiveFile.EncryptedFileInfo.FullName, destinationActiveFile.DecryptedFileInfo.FullName));
                        }
                        return new ActiveFile(destinationActiveFile, key);
                    }
                }
            }
            return null;
        }
    }
}