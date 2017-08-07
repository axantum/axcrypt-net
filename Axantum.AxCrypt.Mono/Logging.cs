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

using Axantum.AxCrypt.Core.Extensions;
using Axantum.AxCrypt.Core.Runtime;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Axantum.AxCrypt.Mono
{
    public class Logging : ILogging
    {
        private TraceSwitch _switch = InitializeTraceSwitch();

        public Logging()
        {
            Trace.Listeners.Add(new DelegateTraceListener("ILoggingListener", TraceMessage));
        }

        private void TraceMessage(string message)
        {
            OnLogging(new LoggingEventArgs(message));
        }

        #region ILogging Members

        public event EventHandler<LoggingEventArgs> Logged;

        protected virtual void OnLogging(LoggingEventArgs e)
        {
            EventHandler<LoggingEventArgs> handler = Logged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public void SetLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Fatal:
                    _switch.Level = TraceLevel.Off;
                    break;

                case LogLevel.Error:
                    _switch.Level = TraceLevel.Error;
                    break;

                case LogLevel.Warning:
                    _switch.Level = TraceLevel.Warning;
                    break;

                case LogLevel.Info:
                    _switch.Level = TraceLevel.Info;
                    break;

                case LogLevel.Debug:
                    _switch.Level = TraceLevel.Verbose;
                    break;

                default:
                    throw new ArgumentException("level must be a value form the LogLevel enumeration.");
            }
        }

        public bool IsFatalEnabled
        {
            get { return _switch != null && _switch.Level >= TraceLevel.Off; }
        }

        public bool IsErrorEnabled
        {
            get { return _switch != null && _switch.Level >= TraceLevel.Error; }
        }

        public bool IsWarningEnabled
        {
            get { return _switch != null && _switch.Level >= TraceLevel.Warning; }
        }

        public bool IsInfoEnabled
        {
            get { return _switch != null && _switch.Level >= TraceLevel.Info; }
        }

        public bool IsDebugEnabled
        {
            get { return _switch != null && _switch.Level >= TraceLevel.Verbose; }
        }

        public virtual void LogFatal(string message)
        {
            if (IsFatalEnabled)
            {
                Trace.WriteLine("{1} Fatal: {0}".InvariantFormat(message, AppName));
            }
        }

        public void LogError(string errorLog)
        {
            if (IsErrorEnabled)
            {
                Trace.TraceError(errorLog);
            }
        }

        public void LogWarning(string warningLog)
        {
            if (IsWarningEnabled)
            {
                Trace.TraceWarning(warningLog);
            }
        }

        public void LogInfo(string infoLog)
        {
            if (IsInfoEnabled)
            {
                Trace.TraceInformation(infoLog);
            }
        }

        public void LogDebug(string debugLog)
        {
            if (IsDebugEnabled)
            {
                Trace.WriteLine("{1} Debug: {0}".InvariantFormat(debugLog, AppName));
            }
        }

        #endregion ILogging Members

        private static TraceSwitch InitializeTraceSwitch()
        {
            TraceSwitch traceSwitch = new TraceSwitch("axCryptSwitch", "Logging levels for AxCrypt");
            traceSwitch.Level = TraceLevel.Error;
            return traceSwitch;
        }

        private static string _appName;

        private static string AppName
        {
            get
            {
                if (_appName == null)
                {
                    _appName = Path.GetFileName(Environment.GetCommandLineArgs()[0]);
                }
                return _appName;
            }
        }

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
            if (_switch != null)
            {
                Trace.Listeners.Remove("ILoggingListener");
                _switch = null;
            }
        }
    }
}