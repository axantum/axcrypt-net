﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Axantum.AxCrypt.Mono.Portable
{
    internal class CryptographyCryptoTransformWrapper : System.Security.Cryptography.ICryptoTransform
    {
        private Axantum.AxCrypt.Core.Portable.ICryptoTransform _cryptoTransform;

        public CryptographyCryptoTransformWrapper(Axantum.AxCrypt.Core.Portable.ICryptoTransform cryptoTransform)
        {
            _cryptoTransform = cryptoTransform;
        }

        public bool CanReuseTransform
        {
            get { return _cryptoTransform.CanReuseTransform; }
        }

        public bool CanTransformMultipleBlocks
        {
            get { return _cryptoTransform.CanTransformMultipleBlocks; }
        }

        public int InputBlockSize
        {
            get { return _cryptoTransform.InputBlockSize; }
        }

        public int OutputBlockSize
        {
            get { return _cryptoTransform.OutputBlockSize; }
        }

        public int TransformBlock(byte[] inputBuffer, int inputOffset, int inputCount, byte[] outputBuffer, int outputOffset)
        {
            return _cryptoTransform.TransformBlock(inputBuffer, inputOffset, inputCount, outputBuffer, outputOffset);
        }

        public byte[] TransformFinalBlock(byte[] inputBuffer, int inputOffset, int inputCount)
        {
            return _cryptoTransform.TransformFinalBlock(inputBuffer, inputOffset, inputCount);
        }

        public void Dispose()
        {
            _cryptoTransform.Dispose();
        }
    }
}