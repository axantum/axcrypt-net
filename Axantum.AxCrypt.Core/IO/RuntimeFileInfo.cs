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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Axantum.AxCrypt.Core.IO
{
    public class RuntimeFileInfo : IRuntimeFileInfo
    {
        private FileInfo _file;

        public RuntimeFileInfo(FileInfo file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }
            _file = new FileInfo(file.FullName);
        }

        public Stream OpenRead()
        {
            return _file.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        public Stream OpenWrite()
        {
            return _file.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }

        public string Name
        {
            get
            {
                return _file.Name;
            }
        }

        public DateTime CreationTimeUtc
        {
            get
            {
                return _file.CreationTimeUtc;
            }
            set
            {
                _file.CreationTimeUtc = value;
            }
        }

        public DateTime LastAccessTimeUtc
        {
            get
            {
                return _file.LastAccessTimeUtc;
            }
            set
            {
                _file.LastAccessTimeUtc = value;
            }
        }

        public DateTime LastWriteTimeUtc
        {
            get
            {
                return _file.LastWriteTimeUtc;
            }
            set
            {
                _file.LastWriteTimeUtc = value;
            }
        }

        public void SetFileTimes(DateTime creationTimeUtc, DateTime lastAccessTimeUtc, DateTime lastWriteTimeUtc)
        {
            CreationTimeUtc = creationTimeUtc;
            LastAccessTimeUtc = lastAccessTimeUtc;
            LastWriteTimeUtc = lastWriteTimeUtc;
        }

        public IRuntimeFileInfo CreateEncryptedName()
        {
            FileInfo encryptedNameFileInfo = _file.CreateEncryptedName();

            return new RuntimeFileInfo(encryptedNameFileInfo);
        }

        public string FullName
        {
            get { return _file.FullName; }
        }

        public bool Exists
        {
            get
            {
                return _file.Exists;
            }
        }

        public void MoveTo(string destFileName)
        {
            _file.MoveTo(destFileName);
        }

        public void Delete()
        {
            _file.Delete();
        }
    }
}