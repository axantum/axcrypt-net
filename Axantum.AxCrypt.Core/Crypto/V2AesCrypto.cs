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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Axantum.AxCrypt.Core.Extensions;

namespace Axantum.AxCrypt.Core.Crypto
{
    /// <summary>
    /// Implements V2 AES Cryptography, briefly AES-256 in CTR-Mode.
    /// </summary>
    public class V2AesCrypto : ICrypto
    {
        private Aes _aes;

        private long _blockCounter;

        private int _blockLength;

        private int _blockOffset;

        /// <summary>
        /// Instantiate a transformation
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="iv">Initial Vector, will be XOR:ed with the counter value</param>
        /// <param name="blockCounter">The block counter.</param>
        /// <param name="blockOffset">The block offset.</param>
        /// <exception cref="System.ArgumentNullException">key
        /// or
        /// iv</exception>
        public V2AesCrypto(AesKey key, AesIV iv, long blockCounter, int blockOffset)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (iv == null)
            {
                throw new ArgumentNullException("iv");
            }

            _aes = new AesManaged();
            _blockLength = _aes.BlockSize / 8;
            if (iv.Length != _blockLength)
            {
                throw new ArgumentException("The IV length must be the same as the algorithm block length.");
            }

            _aes.Key = key.GetBytes();
            _aes.Mode = CipherMode.ECB;
            _aes.IV = iv.GetBytes();
            _aes.Padding = PaddingMode.None;

            _blockCounter = blockCounter;
            _blockOffset = blockOffset;
        }

        public V2AesCrypto(AesKey key, AesIV iv, long keyStreamOffset)
            : this(key, iv, keyStreamOffset / iv.Length, (int)(keyStreamOffset % iv.Length))
        {
        }

        /// <summary>
        /// Decrypt in one operation.
        /// </summary>
        /// <param name="cipherText">The complete cipher text</param>
        /// <returns>
        /// The decrypted result minus any padding
        /// </returns>
        public byte[] Decrypt(byte[] cipherText)
        {
            return Transform(cipherText);
        }

        /// <summary>
        /// Encrypt in one operation
        /// </summary>
        /// <param name="plaintext">The complete plaintext bytes</param>
        /// <returns>
        /// The cipher text, complete with any padding
        /// </returns>
        public byte[] Encrypt(byte[] plaintext)
        {
            return Transform(plaintext);
        }

        private byte[] Transform(byte[] plaintext)
        {
            using (ICryptoTransform transform = new CounterModeCryptoTransform(_aes, _blockCounter, _blockOffset))
            {
                _blockCounter += plaintext.Length / _blockLength;
                _blockOffset += plaintext.Length % _blockLength;
                if (_blockOffset == _blockLength)
                {
                    _blockCounter += 1;
                    _blockOffset = 0;
                }
                return transform.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }
        }

        /// <summary>
        /// Using this instances parameters, create a decryptor
        /// </summary>
        /// <returns>
        /// A new decrypting transformation instance
        /// </returns>
        public ICryptoTransform CreateDecryptingTransform()
        {
            return new CounterModeCryptoTransform(_aes, _blockCounter, _blockOffset);
        }

        /// <summary>
        /// Using this instances parameters, create an encryptor
        /// </summary>
        /// <returns>
        /// A new encrypting transformation instance
        /// </returns>
        public ICryptoTransform CreateEncryptingTransform()
        {
            return new CounterModeCryptoTransform(_aes, _blockCounter, _blockOffset);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DisposeInternal();
            }
        }

        private void DisposeInternal()
        {
            if (_aes != null)
            {
                _aes.Clear();
                _aes = null;
            }
        }
    }
}