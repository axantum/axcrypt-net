﻿using Axantum.AxCrypt.Core.Runtime;
using Axantum.AxCrypt.Core.UI;
using System;
using System.IO;
using System.Linq;

namespace Axantum.AxCrypt.Core.Portable
{
    public interface IPortableFactory
    {
        HashAlgorithm SHA1Managed();

        HashAlgorithm SHA256Managed();

        RandomNumberGenerator RandomNumberGenerator();

        ISemaphore Semaphore(int initialCount, int maximumCoiunt);

        IPath Path();

        IThreadWorker ThreadWorker(IProgressContext progress, bool startSerializedOnUIThread);

        ISingleThread SingleThread();
    }
}