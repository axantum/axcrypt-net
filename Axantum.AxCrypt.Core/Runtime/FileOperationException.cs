﻿#region Coypright and License

/*
 * AxCrypt - Copyright 2016, Svante Seleborg, All Rights Reserved
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
 * http://www.axcrypt.net for more information about the author.
*/

#endregion Coypright and License

using Axantum.AxCrypt.Abstractions;
using System;

namespace Axantum.AxCrypt.Core.Runtime
{
    /// <summary>
    /// A file access operation has failed, perhaps due to insufficient permissions, or space etc.
    /// </summary>
    public class FileOperationException : AxCryptException
    {
        public FileOperationException()
            : base()
        {
        }

        public FileOperationException(string message)
            : base(message, ErrorStatus.InternalError)
        {
        }

        public FileOperationException(string message, ErrorStatus errorStatus)
            : base(message, errorStatus)
        {
        }

        public FileOperationException(string message, string fullName, ErrorStatus errorStatus)
            : base(message, errorStatus)
        {
            DisplayContext = fullName;
        }

        public FileOperationException(string message, Exception innerException)
            : this(message, ErrorStatus.InternalError, innerException)
        {
        }

        public FileOperationException(string message, ErrorStatus errorStatus, Exception innerException)
            : base(message, errorStatus, innerException)
        {
        }

        public FileOperationException(string message, string fullName, ErrorStatus errorStatus, Exception innerException)
            : base(message, errorStatus, innerException)
        {
            DisplayContext = fullName;
        }
    }
}