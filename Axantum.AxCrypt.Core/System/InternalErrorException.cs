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
using System.Runtime.Serialization;

namespace Axantum.AxCrypt.Core.System
{
    /// <summary>
    /// An internal program logic error in the library itself has been detected. Use InvalidOperationException for invalid
    /// program states typically caused by caller errors.
    /// </summary>
    [Serializable]
    public class InternalErrorException : AxCryptException
    {
        public InternalErrorException()
            : base()
        {
        }

        public InternalErrorException(string message)
            : base(message, ErrorStatus.InternalError)
        {
        }

        public InternalErrorException(string message, ErrorStatus errorStatus)
            : base(message, errorStatus)
        {
        }

        protected InternalErrorException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public InternalErrorException(string message, Exception innerException)
            : this(message, ErrorStatus.InternalError, innerException)
        {
        }

        public InternalErrorException(string message, ErrorStatus errorStatus, Exception innerException)
            : base(message, errorStatus, innerException)
        {
        }
    }
}