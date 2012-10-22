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
using System.Linq;
using System.Text;
using System.Threading;
using Axantum.AxCrypt.Core.UI;

namespace Axantum.AxCrypt.Core.Runtime
{
    /// <summary>
    /// Manage a group of worker threads with a maximum level of concurrency.
    /// </summary>
    public class WorkerGroup : IDisposable
    {
        private class ThreadWorkerWrapper : IThreadWorker
        {
            private IThreadWorker _worker;

            public ThreadWorkerWrapper(IThreadWorker worker)
            {
                _worker = worker;
            }

            #region IThreadWorker Members

            /// <summary>
            /// Start the asynchronous execution of the work.
            /// </summary>
            public void Run()
            {
                _worker.Run();
            }

            /// <summary>
            /// Perform blocking wait until this thread has completed execution.
            /// </summary>
            public void Join()
            {
                throw new InvalidOperationException("This instance is managed by a WorkerGroup, and Join() cannot be called explicitly.");
            }

            /// <summary>
            /// Abort this thread - can only be called *before* Run() has been called.
            /// </summary>
            public void Abort()
            {
                _worker.Abort();
            }

            /// <summary>
            /// Returns true if the thread has completed execution.
            /// </summary>
            public bool HasCompleted
            {
                get { throw new InvalidOperationException("This instance is managed by a WorkerGroup, and HasCompleted cannot be called explicitly."); }
            }

            /// <summary>
            /// Raised just before asynchronous execution starts. Runs on the
            /// original thread, typically the GUI thread.
            /// </summary>
            public event EventHandler<ThreadWorkerEventArgs> Prepare
            {
                add { _worker.Prepare += value; }
                remove { _worker.Prepare -= value; }
            }

            /// <summary>
            /// Raised when asynchronous execution starts. Runs on a different
            /// thread than the caller thread. Do not interact with the GUI here.
            /// </summary>
            public event EventHandler<ThreadWorkerEventArgs> Work
            {
                add { _worker.Work += value; }
                remove { _worker.Work -= value; }
            }

            /// <summary>
            /// Raised when all is done. Runs on the original thread, typically
            /// the GUI thread.
            /// </summary>
            public event EventHandler<ThreadWorkerEventArgs> Completed
            {
                add { _worker.Completed += value; }
                remove { _worker.Completed -= value; }
            }

            #endregion IThreadWorker Members
        }

        private Semaphore _concurrencyControlSemaphore;

        private int _maxConcurrencyCount;

        private bool _disposed = false;

        private bool _finished = false;

        private readonly object _finishedLock = new object();

        /// <summary>
        /// Instantiate a worker group with no concurrency and implicit progress reporting. Progress
        /// will be reported on the thread calling the constructor.
        /// </summary>
        public WorkerGroup()
            : this(1)
        {
        }

        /// <summary>
        /// Instantiates a worker group with specified maximum concurrency and implicit progress reporting. Progress
        /// will be reported on the thread calling the constructor.
        /// </summary>
        /// <param name="maxConcurrent">The maximum number of worker threads active at any one time</param>
        public WorkerGroup(int maxConcurrent)
            : this(maxConcurrent, new ProgressContext())
        {
        }

        /// <summary>
        /// Instantiates a worker group with specified maximum concurrency and external progress reporting. Progress
        /// will be reported on the thread instantiating the ProgressContext used.
        /// </summary>
        /// <param name="maxConcurrent">The maximum number of worker threads active at any one time</param>
        /// <param name="progress">The ProgressContext that receives progress notifications</param>
        public WorkerGroup(int maxConcurrent, ProgressContext progress)
        {
            _concurrencyControlSemaphore = new Semaphore(maxConcurrent, maxConcurrent);
            _maxConcurrencyCount = maxConcurrent;
            FirstError = FileOperationStatus.Success;
            Progress = progress;
        }

        /// <summary>
        /// The ProgressContext that is passed to worker threads for progress reporting and cancellation checks. If it was
        /// instantiated by this instance, then progress events are subscribed to and forwarded to the original instantiating
        /// thread, typically the GUI thread. If the ProgressContext was supplied explicitly by the caller when instantiating this
        /// instance, no progress events are subscribed to by this instance.
        /// </summary>
        public ProgressContext Progress { get; private set; }

        /// <summary>
        /// Notify this instance that all work has been scheduled. This call will block until all executing threads have terminated.
        /// It will also notify the ProgressContext that all work has finished.
        /// </summary>
        /// <remarks>
        /// Since this call may block, it should never be called from the GUI thread.
        /// </remarks>
        public void NotifyFinished()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("WorkerGroup");
            }
            lock (_finishedLock)
            {
                if (_finished)
                {
                    throw new InvalidOperationException("NotifyFinished() must only be called once, but was called twice.");
                }
                NotifyFinishedInternal();
            }
        }

        public static void FinishInBackground(WorkerGroup workerGroup)
        {
            ThreadPool.QueueUserWorkItem(
                (object state) =>
                {
                    WorkerGroup group = (WorkerGroup)state;
                    try
                    {
                        group.NotifyFinished();
                    }
                    finally
                    {
                        group.Dispose();
                    }
                },
                workerGroup);
        }

        private void NotifyFinishedInternal()
        {
            lock (_finishedLock)
            {
                if (_finished)
                {
                    return;
                }

                for (int i = 0; i < _maxConcurrencyCount; ++i)
                {
                    AcquireOneConcurrencyRight();
                }
                lock (_threadWorkers)
                {
                    for (int i = _threadWorkers.Count - 1; i >= 0; --i)
                    {
                        _threadWorkers[i].Join();
                    }
                }
                _finished = true;
            }
            Progress.NotifyFinished();
        }

        private void DisposeCompletedWorkerThreads()
        {
            lock (_threadWorkers)
            {
                for (int i = _threadWorkers.Count - 1; i >= 0; --i)
                {
                    if (_threadWorkers[i].HasCompleted)
                    {
                        _threadWorkers[i].Dispose();
                        _threadWorkers.RemoveAt(i);
                    }
                }
            }
        }

        private List<ThreadWorker> _threadWorkers = new List<ThreadWorker>();

        /// <summary>
        /// Create a ThreadWorker for background work. Concurrency limitations are effective, so this call may block.
        /// </summary>
        /// <remarks>
        /// Since this call may block, it should not be called from the GUI thread if there is a risk of blocking.
        /// </remarks>
        /// <returns></returns>
        public IThreadWorker CreateWorker()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException("WorkerGroup");
            }
            AcquireOneConcurrencyRight();
            DisposeCompletedWorkerThreads();
            ThreadWorker threadWorker = new ThreadWorker(Progress);
            threadWorker.Completed += new EventHandler<ThreadWorkerEventArgs>(HandleThreadWorkerCompleted);
            lock (_threadWorkers)
            {
                _threadWorkers.Add(threadWorker);
            }
            return new ThreadWorkerWrapper(threadWorker);
        }

        private void HandleThreadWorkerCompleted(object sender, ThreadWorkerEventArgs e)
        {
            if (e.Result != FileOperationStatus.Success)
            {
                lock (_firstErrorLock)
                {
                    if (FirstError == FileOperationStatus.Success)
                    {
                        FirstError = e.Result;
                    }
                }
            }
            ReleaseOneConcurrencyRight();
            DisposeCompletedWorkerThreads();
        }

        private static readonly object _firstErrorLock = new object();

        /// <summary>
        /// The first error reported. Subsequent errors may be missed.
        /// </summary>
        public FileOperationStatus FirstError { get; private set; }

        private void AcquireOneConcurrencyRight()
        {
            _concurrencyControlSemaphore.WaitOne();
        }

        private void ReleaseOneConcurrencyRight()
        {
            _concurrencyControlSemaphore.Release();
        }

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable Members

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                NotifyFinishedInternal();
                DisposeCompletedWorkerThreads();
                if (_concurrencyControlSemaphore != null)
                {
                    _concurrencyControlSemaphore.Close();
                    _concurrencyControlSemaphore = null;
                }
            }
            _disposed = true;
        }
    }
}