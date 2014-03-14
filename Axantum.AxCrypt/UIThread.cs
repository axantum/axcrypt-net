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

using Axantum.AxCrypt.Core.UI;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Axantum.AxCrypt
{
    public class UIThread : IUIThread
    {
        private Control _control;

        private SynchronizationContext _context;

        public UIThread(Control control)
        {
            _control = control;
            _context = SynchronizationContext.Current;
        }

        public bool IsOnUIThread
        {
            get { return !_control.InvokeRequired; }
        }

        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes", Justification = "This is to marshal the exception possibly between threads and then throw a new one.")]
        public void RunOnUIThread(Action action)
        {
            if (IsOnUIThread)
            {
                action();
                return;
            }
            Exception exception = null;
            _context.Send((state) => { try { action(); } catch (Exception ex) { exception = ex; } }, null);
            if (exception != null)
            {
                throw new InvalidOperationException("Exception on UI Thread", exception);
            }
        }
    }
}