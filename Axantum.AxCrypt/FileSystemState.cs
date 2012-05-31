﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Axantum.AxCrypt.Core;
using Axantum.AxCrypt.Core.IO;

namespace Axantum.AxCrypt
{
    [DataContract(Namespace = "http://www.axantum.com/Serialization/")]
    public class FileSystemState
    {
        private object _lock;

        private FileSystemState()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lock = new object();
        }

        private Dictionary<string, ActiveFile> _activeFilesByEncryptedPath = new Dictionary<string, ActiveFile>();

        private Dictionary<string, ActiveFile> _activeFilesByDecryptedPath = new Dictionary<string, ActiveFile>();

        public event EventHandler<EventArgs> Changed;

        protected virtual void OnChanged(EventArgs e)
        {
            EventHandler<EventArgs> handler = Changed;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public IEnumerable<ActiveFile> ActiveFiles
        {
            get
            {
                return _activeFilesByDecryptedPath.Values;
            }
            set
            {
                lock (_lock)
                {
                    SetRangeInternal(value, ActiveFileStatus.None);
                }
            }
        }

        public ActiveFile FindEncryptedPath(string encryptedPath)
        {
            ActiveFile activeFile;
            lock (_lock)
            {
                if (_activeFilesByEncryptedPath.TryGetValue(encryptedPath, out activeFile))
                {
                    return activeFile;
                }
            }
            return null;
        }

        public ActiveFile FindDecryptedPath(string decryptedPath)
        {
            ActiveFile activeFile;
            lock (_lock)
            {
                if (_activeFilesByDecryptedPath.TryGetValue(decryptedPath, out activeFile))
                {
                    return activeFile;
                }
            }
            return null;
        }

        public void Add(ActiveFile activeFile)
        {
            lock (_lock)
            {
                AddInternal(activeFile);
            }
        }

        public void Remove(ActiveFile activeFile)
        {
            lock (_lock)
            {
                _activeFilesByDecryptedPath.Remove(activeFile.DecryptedPath);
                _activeFilesByEncryptedPath.Remove(activeFile.EncryptedPath);
            }
        }

        private void AddInternal(ActiveFile activeFile)
        {
            _activeFilesByEncryptedPath[activeFile.EncryptedPath] = activeFile;
            _activeFilesByDecryptedPath[activeFile.DecryptedPath] = activeFile;
        }

        [DataMember(Name = "ActiveFiles")]
        private ICollection<ActiveFile> ActiveFilesForSerialization
        {
            get
            {
                return new ActiveFileCollection(_activeFilesByEncryptedPath.Values);
            }
            set
            {
                SetRangeInternal(value, ActiveFileStatus.Error | ActiveFileStatus.IgnoreChange | ActiveFileStatus.NotShareable);
            }
        }

        private void SetRangeInternal(IEnumerable<ActiveFile> activeFiles, ActiveFileStatus mask)
        {
            _activeFilesByDecryptedPath = new Dictionary<string, ActiveFile>();
            _activeFilesByEncryptedPath = new Dictionary<string, ActiveFile>();
            foreach (ActiveFile activeFile in activeFiles)
            {
                ActiveFile thisActiveFile = activeFile;
                if ((activeFile.Status & mask) != 0)
                {
                    thisActiveFile = new ActiveFile(activeFile, activeFile.Status & ~mask, null);
                }
                AddInternal(thisActiveFile);
            }
        }

        private string _path;

        public string DirectoryPath
        {
            get { return _path; }
        }

        public static FileSystemState Load(IRuntimeFileInfo path)
        {
            if (!path.Exists)
            {
                FileSystemState state = new FileSystemState();
                state._path = path.FullName;
                if (Logging.IsInfoEnabled)
                {
                    Logging.Info("No existing FileSystemState. Save location is '{0}'.".InvariantFormat(state._path));
                }
                return state;
            }

            DataContractSerializer serializer = CreateSerializer();
            using (FileStream fileSystemStateStream = new FileStream(path.FullName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                FileSystemState fileSystemState = (FileSystemState)serializer.ReadObject(fileSystemStateStream);
                fileSystemState._path = path.FullName;
                if (Logging.IsInfoEnabled)
                {
                    Logging.Info("Loaded FileSystemState from '{0}'.".InvariantFormat(fileSystemState._path));
                }
                return fileSystemState;
            }
        }

        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Initialize();
        }

        public void Save()
        {
            using (FileStream fileSystemStateStream = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (_lock)
                {
                    DataContractSerializer serializer = CreateSerializer();
                    serializer.WriteObject(fileSystemStateStream, this);
                }
            }
            if (Logging.IsInfoEnabled)
            {
                Logging.Info("Wrote FileSystemState to '{0}'.".InvariantFormat(_path));
            }
        }

        private static DataContractSerializer CreateSerializer()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(FileSystemState), "FileSystemState", "http://www.axantum.com/Serialization/");
            return serializer;
        }
    }
}