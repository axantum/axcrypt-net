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
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Axantum.AxCrypt.Core;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.IO;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.System;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Mono;
using Axantum.AxCrypt.Properties;

namespace Axantum.AxCrypt
{
    /// <summary>
    /// All code here is guaranteed to execute on the GUI thread. If code may be called on another thread, this call
    /// should be made through MainFormThreadFacade .
    /// </summary>
    public partial class AxCryptMainForm : Form
    {
        private Uri _updateUrl = Settings.Default.UpdateUrl;

        public static MessageBoxOptions MessageBoxOptions { get; private set; }

        public void FormatTraceMessage(string message)
        {
            ThreadSafeUi(() =>
            {
                string formatted = "{0} {1}".InvariantFormat(OS.Current.UtcNow.ToString("o", CultureInfo.InvariantCulture), message.TrimLogMessage()); //MLHIDE
                LogOutput.AppendText(formatted);
            });
        }

        private FileSystemState FileSystemState { get; set; }

        public AxCryptMainForm()
        {
            OS.Current = new RuntimeEnvironment();
            InitializeComponent();
        }

        private void AxCryptMainForm_Load(object sender, EventArgs e)
        {
            if (DesignMode)
            {
                return;
            }

            Trace.Listeners.Add(new DelegateTraceListener("AxCryptMainFormListener", FormatTraceMessage)); //MLHIDE

            TrackProcess = OS.Current.Platform == Platform.WindowsDesktop;

            RestoreUserPreferences();

            Text = "{0} {1}{2}".InvariantFormat(Application.ProductName, Application.ProductVersion, String.IsNullOrEmpty(AboutBox.AssemblyDescription) ? String.Empty : " " + AboutBox.AssemblyDescription);

            MessageBoxOptions = RightToLeft == RightToLeft.Yes ? MessageBoxOptions.RightAlign | MessageBoxOptions.RtlReading : 0;

            OS.Current.FileChanged += new EventHandler<EventArgs>(FileSystemOrStateChanged);

            FileSystemState = new FileSystemState();
            FileSystemState.Changed += new EventHandler<ActiveFileChangedEventArgs>(ActiveFileChanged);

            string fileSystemStateFullName = Path.Combine(OS.Current.TemporaryDirectoryInfo.FullName, "FileSystemState.xml"); //MLHIDE
            FileSystemState.Load(OS.Current.FileInfo(fileSystemStateFullName));

            BackgroundMonitor.UpdateCheck.VersionUpdate += new EventHandler<VersionEventArgs>(VersionUpdated);
            UpdateCheck(Settings.Default.LastUpdateCheckUtc);
        }

        private void ThreadSafeUi(Action action)
        {
            if (InvokeRequired)
            {
                BeginInvoke(action);
            }
            else
            {
                action();
            }
        }

        private void UpdateCheck(DateTime lastCheckUtc)
        {
            BackgroundMonitor.UpdateCheck.CheckInBackground(lastCheckUtc, Settings.Default.NewestKnownVersion, Settings.Default.AxCrypt2VersionCheckUrl, Settings.Default.UpdateUrl);
        }

        private void UpdateVersionStatus(VersionUpdateStatus status, Version version)
        {
            switch (status)
            {
                case VersionUpdateStatus.IsUpToDateOrRecentlyChecked:
                    UpdateToolStripButton.ToolTipText = Axantum.AxCrypt.Properties.Resources.NoNeedToCheckForUpdatesTooltip;
                    UpdateToolStripButton.Enabled = false;
                    break;

                case VersionUpdateStatus.LongTimeSinceLastSuccessfulCheck:
                    UpdateToolStripButton.ToolTipText = Axantum.AxCrypt.Properties.Resources.OldVersionTooltip;
                    UpdateToolStripButton.Image = Resources.refreshred;
                    UpdateToolStripButton.Enabled = true;
                    break;

                case VersionUpdateStatus.NewerVersionIsAvailable:
                    UpdateToolStripButton.ToolTipText = Axantum.AxCrypt.Properties.Resources.NewVersionIsAvailableTooltip.InvariantFormat(version);
                    UpdateToolStripButton.Image = Resources.refreshred;
                    UpdateToolStripButton.Enabled = true;
                    break;

                case VersionUpdateStatus.ShortTimeSinceLastSuccessfulCheck:
                    UpdateToolStripButton.ToolTipText = Axantum.AxCrypt.Properties.Resources.ClickToCheckForNewerVersionTooltip;
                    UpdateToolStripButton.Image = Resources.refreshgreen;
                    UpdateToolStripButton.Enabled = true;
                    break;
            }
        }

        private void VersionUpdated(object sender, VersionEventArgs e)
        {
            Settings.Default.LastUpdateCheckUtc = OS.Current.UtcNow;
            Settings.Default.NewestKnownVersion = e.Version.ToString();
            Settings.Default.Save();
            _updateUrl = e.UpdateWebpageUrl;
            ThreadSafeUi(() => { UpdateVersionStatus(e.VersionUpdateStatus, e.Version); });
        }

        private void FileSystemOrStateChanged(object sender, EventArgs e)
        {
            ThreadSafeUi(RestartTimer);
        }

        private void ActiveFileChanged(object sender, ActiveFileChangedEventArgs e)
        {
            ThreadSafeUi(() => { UpdateActiveFilesViews(e.ActiveFile); });
        }

        private void AxCryptMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (DesignMode)
            {
                return;
            }

            progressBackgroundWorker.WaitForBackgroundIdle();
            TrackProcess = false;
            PurgeActiveFiles();
            progressBackgroundWorker.WaitForBackgroundIdle();
            Trace.Listeners.Remove("AxCryptMainFormListener");        //MLHIDE
        }

        private bool _trackProcess;

        public bool TrackProcess
        {
            get
            {
                return _trackProcess;
            }
            set
            {
                _trackProcess = value;
                if (OS.Log.IsInfoEnabled)
                {
                    OS.Log.LogInfo("ActiveFileMonitor.TrackProcess='{0}'".InvariantFormat(value)); //MLHIDE
                }
            }
        }

        private void RestoreUserPreferences()
        {
            RecentFilesListView.Columns[0].Name = "DecryptedFile";    //MLHIDE
            RecentFilesListView.Columns[0].Width = Settings.Default.RecentFilesDocumentWidth > 0 ? Settings.Default.RecentFilesDocumentWidth : RecentFilesListView.Columns[0].Width;

            UpdateDebugMode();
        }

        public void RestartTimer()
        {
            ActiveFilePolling.Enabled = false;
            ActiveFilePolling.Interval = 1000;
            ActiveFilePolling.Enabled = true;
        }

        private void UpdateActiveFilesViews(ActiveFile activeFile)
        {
            if (activeFile.Status.HasMask(ActiveFileStatus.NoLongerActive))
            {
                OpenFilesListView.Items.RemoveByKey(activeFile.EncryptedFileInfo.FullName);
                RecentFilesListView.Items.RemoveByKey(activeFile.EncryptedFileInfo.FullName);
                return;
            }

            if (activeFile.Status.HasMask(ActiveFileStatus.NotDecrypted))
            {
                UpdateRecentFilesListView(activeFile);
                return;
            }

            if (activeFile.Status.HasMask(ActiveFileStatus.DecryptedIsPendingDelete) || activeFile.Status.HasMask(ActiveFileStatus.AssumedOpenAndDecrypted))
            {
                UpdateOpenFilesListView(activeFile);
                return;
            }
        }

        private void UpdateOpenFilesListView(ActiveFile activeFile)
        {
            ListViewItem item;
            item = new ListViewItem(Path.GetFileName(activeFile.DecryptedFileInfo.FullName), activeFile.Key != null ? "ActiveFile" : "Exclamation"); //MLHIDE
            ListViewItem.ListViewSubItem encryptedPathColumn = new ListViewItem.ListViewSubItem();
            encryptedPathColumn.Name = "EncryptedPath";           //MLHIDE
            encryptedPathColumn.Text = activeFile.EncryptedFileInfo.FullName;
            item.SubItems.Add(encryptedPathColumn);

            item.Name = activeFile.EncryptedFileInfo.FullName;
            OpenFilesListView.Items.RemoveByKey(item.Name);
            OpenFilesListView.Items.Add(item);
            RecentFilesListView.Items.RemoveByKey(item.Name);
        }

        private void UpdateRecentFilesListView(ActiveFile activeFile)
        {
            ListViewItem item;
            if (String.IsNullOrEmpty(activeFile.DecryptedFileInfo.FullName))
            {
                item = new ListViewItem(String.Empty, "InactiveFile"); //MLHIDE
            }
            else
            {
                item = new ListViewItem(Path.GetFileName(activeFile.DecryptedFileInfo.FullName), "ActiveFile"); //MLHIDE
            }

            ListViewItem.ListViewSubItem dateColumn = new ListViewItem.ListViewSubItem();
            dateColumn.Text = activeFile.LastActivityTimeUtc.ToLocalTime().ToString(CultureInfo.CurrentCulture);
            dateColumn.Tag = activeFile.LastActivityTimeUtc;
            dateColumn.Name = "Date";                             //MLHIDE
            item.SubItems.Add(dateColumn);

            ListViewItem.ListViewSubItem encryptedPathColumn = new ListViewItem.ListViewSubItem();
            encryptedPathColumn.Name = "EncryptedPath";           //MLHIDE
            encryptedPathColumn.Text = activeFile.EncryptedFileInfo.FullName;
            item.SubItems.Add(encryptedPathColumn);

            item.Name = activeFile.EncryptedFileInfo.FullName;
            RecentFilesListView.Items.RemoveByKey(item.Name);
            RecentFilesListView.Items.Add(item);
            OpenFilesListView.Items.RemoveByKey(item.Name);
        }

        private void toolStripButtonEncrypt_Click(object sender, EventArgs e)
        {
            EncryptFilesViaDialog();
        }

        private void EncryptFilesViaDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = Resources.EncryptFileOpenDialogTitle;
                ofd.Multiselect = true;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                DialogResult result = ofd.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                foreach (string file in ofd.FileNames)
                {
                    EncryptFile(file);
                }
            }
        }

        private void EncryptFile(string file)
        {
            FileOperationsController operationsController = new FileOperationsController(FileSystemState);

            operationsController.SaveFileRequest += (object sender, FileOperationEventArgs e) =>
            {
                using (SaveFileDialog sfd = new SaveFileDialog())
                {
                    sfd.Title = Resources.EncryptFileSaveAsDialogTitle;
                    sfd.AddExtension = true;
                    sfd.ValidateNames = true;
                    sfd.CheckPathExists = true;
                    sfd.DefaultExt = OS.Current.AxCryptExtension;
                    sfd.FileName = e.SaveFileName;
                    sfd.Filter = Resources.EncryptedFileDialogFilterPattern.InvariantFormat(OS.Current.AxCryptExtension);
                    sfd.InitialDirectory = Path.GetDirectoryName(e.SaveFileName);
                    sfd.ValidateNames = true;
                    DialogResult saveAsResult = sfd.ShowDialog();
                    if (saveAsResult != DialogResult.OK)
                    {
                        e.Cancel = true;
                        return;
                    }
                    e.SaveFileName = sfd.FileName;
                }
            };

            operationsController.EncryptionPassphraseRequest += (object sender, FileOperationEventArgs e) =>
            {
                EncryptPassphraseDialog passphraseDialog = new EncryptPassphraseDialog();
                passphraseDialog.ShowPassphraseCheckBox.Checked = Settings.Default.ShowEncryptPasshrase;
                DialogResult dialogResult = passphraseDialog.ShowDialog();
                if (dialogResult != DialogResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
                if (passphraseDialog.ShowPassphraseCheckBox.Checked != Settings.Default.ShowEncryptPasshrase)
                {
                    Settings.Default.ShowEncryptPasshrase = passphraseDialog.ShowPassphraseCheckBox.Checked;
                    Settings.Default.Save();
                }
                e.Passphrase = passphraseDialog.PassphraseTextBox.Text;
                FileSystemState.KnownKeys.DefaultEncryptionKey = new Passphrase(e.Passphrase).DerivedPassphrase;
            };

            operationsController.FileOperationRequest += (object sender, FileOperationEventArgs e) =>
            {
                progressBackgroundWorker.BackgroundWorkWithProgress(file,
                    (ProgressContext progressContext) =>
                    {
                        e.Progress = progressContext;
                        return operationsController.DoOperation(e);
                    },
                    (FileOperationStatus status) =>
                    {
                        CheckStatusAndShowMessage(status, file);
                    });
            };

            operationsController.EncryptFile(file);
        }

        private static void CheckStatusAndShowMessage(FileOperationStatus status, string displayText)
        {
            switch (status)
            {
                case FileOperationStatus.Success:
                    break;

                case FileOperationStatus.UnspecifiedError:
                    Resources.FileOperationFailed.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.FileAlreadyExists:
                    Resources.FileAlreadyExists.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.FileDoesNotExist:
                    Resources.FileDoesNotExist.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.CannotWriteDestination:
                    Resources.CannotWrite.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.CannotStartApplication:
                    Resources.CannotStartApplication.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.InconsistentState:
                    Resources.InconsistentState.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.InvalidKey:
                    Resources.InvalidKey.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.Canceled:
                    Resources.Canceled.InvariantFormat(displayText).ShowWarning();
                    break;

                case FileOperationStatus.Exception:
                    Resources.Exception.InvariantFormat(displayText).ShowWarning();
                    break;
                default:
                    Resources.UnrecognizedError.InvariantFormat(displayText).ShowWarning();
                    break;
            }
        }

        private void toolStripButtonDecrypt_Click(object sender, EventArgs e)
        {
            DecryptFilesViaDialog();
        }

        private void DecryptFilesViaDialog()
        {
            string[] fileNames;
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = Resources.DecryptFileOpenDialogTitle;
                ofd.Multiselect = true;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = OS.Current.AxCryptExtension;
                ofd.Filter = Resources.EncryptedFileDialogFilterPattern.InvariantFormat("{0}".InvariantFormat(OS.Current.AxCryptExtension)); //MLHIDE
                ofd.Multiselect = true;
                DialogResult result = ofd.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                fileNames = ofd.FileNames;
            }
            foreach (string file in fileNames)
            {
                if (!DecryptFile(OS.Current.FileInfo(file)))
                {
                    return;
                }
            }
        }

        private bool DecryptFile(IRuntimeFileInfo source)
        {
            FileOperationsController operationsController = new FileOperationsController(FileSystemState);

            operationsController.DecryptionPassphraseRequest += HandleDecryptionPassphraseRequest;

            operationsController.SaveFileRequest += (object sender, FileOperationEventArgs e) =>
            {
                string extension = Path.GetExtension(e.SaveFileName);
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.AddExtension = !String.IsNullOrEmpty(extension);
                sfd.CheckPathExists = true;
                sfd.DefaultExt = extension;
                sfd.Filter = Resources.DecryptedSaveAsFileDialogFilterPattern.InvariantFormat(extension);
                sfd.InitialDirectory = Path.GetDirectoryName(source.FullName);
                sfd.OverwritePrompt = true;
                sfd.RestoreDirectory = true;
                sfd.Title = Resources.DecryptedSaveAsFileDialogTitle;
                DialogResult result = sfd.ShowDialog();
                if (result != DialogResult.OK)
                {
                    e.Cancel = true;
                    return;
                }
                e.SaveFileName = sfd.FileName;
                return;
            };

            operationsController.KnownKeyAdded += (object sender, FileOperationEventArgs e) =>
            {
                AddKnownKey(e.Key);
            };

            operationsController.FileOperationRequest += (object sender, FileOperationEventArgs e) =>
            {
                progressBackgroundWorker.BackgroundWorkWithProgress(source.Name,
                    (ProgressContext progressContext) =>
                    {
                        e.Progress = progressContext;
                        return operationsController.DoOperation(e);
                    },
                    (FileOperationStatus status) =>
                    {
                        CheckStatusAndShowMessage(status, source.Name);
                    });
            };

            return operationsController.DecryptFile(source.FullName);
        }

        private void AddKnownKey(AesKey key)
        {
            FileSystemState.KnownKeys.Add(key);
            FileSystemState.CheckActiveFiles(ChangedEventMode.RaiseOnlyOnModified, TrackProcess, new ProgressContext());
        }

        private void toolStripButtonOpenEncrypted_Click(object sender, EventArgs e)
        {
            OpenDialog();
        }

        private void OpenDialog()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = Resources.OpenEncryptedFileOpenDialogTitle;
                ofd.Multiselect = false;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = OS.Current.AxCryptExtension;
                ofd.Filter = Resources.EncryptedFileDialogFilterPattern.InvariantFormat("{0}".InvariantFormat(OS.Current.AxCryptExtension)); //MLHIDE
                DialogResult result = ofd.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }

                foreach (string file in ofd.FileNames)
                {
                    if (!OpenEncrypted(file))
                    {
                        return;
                    }
                }
            }
        }

        private bool OpenEncrypted(string file)
        {
            FileOperationsController operationsController = new FileOperationsController(FileSystemState);

            operationsController.DecryptionPassphraseRequest += HandleDecryptionPassphraseRequest;

            operationsController.KnownKeyAdded += (object sender, FileOperationEventArgs e) =>
            {
                AddKnownKey(e.Key);
            };

            operationsController.FileOperationRequest += (object sender, FileOperationEventArgs e) =>
            {
                progressBackgroundWorker.BackgroundWorkWithProgress(file,
                    (ProgressContext progressContext) =>
                    {
                        e.Progress = progressContext;
                        return operationsController.DoOperation(e);
                    },
                    (FileOperationStatus status) =>
                    {
                        CheckStatusAndShowMessage(status, file);
                    });
            };

            return operationsController.DecryptAndLaunch(file);
        }

        private void HandleDecryptionPassphraseRequest(object sender, FileOperationEventArgs e)
        {
            string passphraseText = AskForDecryptPassphrase();
            if (passphraseText == null)
            {
                e.Cancel = true;
                return;
            }
            e.Passphrase = passphraseText;
        }

        private string AskForDecryptPassphrase()
        {
            DecryptPassphraseDialog passphraseDialog = new DecryptPassphraseDialog();
            passphraseDialog.ShowPassphraseCheckBox.Checked = Settings.Default.ShowDecryptPassphrase;
            DialogResult dialogResult = passphraseDialog.ShowDialog(this);
            if (dialogResult != DialogResult.OK)
            {
                return null;
            }
            if (passphraseDialog.ShowPassphraseCheckBox.Checked != Settings.Default.ShowDecryptPassphrase)
            {
                Settings.Default.ShowDecryptPassphrase = passphraseDialog.ShowPassphraseCheckBox.Checked;
                Settings.Default.Save();
            }
            return passphraseDialog.Passphrase.Text;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private bool _pollingInProgress = false;

        private void ActiveFilePolling_Tick(object sender, EventArgs e)
        {
            if (OS.Log.IsInfoEnabled)
            {
                OS.Log.LogInfo("Tick");                                 //MLHIDE
            }
            if (_pollingInProgress)
            {
                return;
            }
            ActiveFilePolling.Enabled = false;
            try
            {
                _pollingInProgress = true;
                progressBackgroundWorker.BackgroundWorkWithProgress(Resources.UpdatingStatus,
                    (ProgressContext progress) =>
                    {
                        FileSystemState.CheckActiveFiles(ChangedEventMode.RaiseOnlyOnModified, TrackProcess, progress);
                        return FileOperationStatus.Success;
                    },
                    (FileOperationStatus status) =>
                    {
                        CloseAndRemoveOpenFilesButton.Enabled = OpenFilesListView.Items.Count > 0;
                    });
            }
            finally
            {
                _pollingInProgress = false;
            }
        }

        private void openEncryptedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenDialog();
        }

        private void RecentFilesListView_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void RecentFilesListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string encryptedPath = RecentFilesListView.SelectedItems[0].SubItems["EncryptedPath"].Text; //MLHIDE

            OpenEncrypted(encryptedPath);
        }

        private void RecentFilesListView_ColumnWidthChanged(object sender, ColumnWidthChangedEventArgs e)
        {
            ListView listView = (ListView)sender;
            string columnName = listView.Columns[e.ColumnIndex].Name;
            switch (columnName)
            {
                case "DecryptedFile":                                 //MLHIDE
                    Settings.Default.RecentFilesDocumentWidth = listView.Columns[e.ColumnIndex].Width;
                    break;
            }
            Settings.Default.Save();
        }

        private void PurgeActiveFiles()
        {
            progressBackgroundWorker.BackgroundWorkWithProgress(Resources.PurgingActiveFiles,
                (ProgressContext progress) =>
                {
                    FileSystemState.CheckActiveFiles(ChangedEventMode.RaiseOnlyOnModified, TrackProcess, progress);
                    FileSystemState.PurgeActiveFiles(progress);
                    return FileOperationStatus.Success;
                },
                (FileOperationStatus status) =>
                {
                    if (status != FileOperationStatus.Success)
                    {
                        CheckStatusAndShowMessage(status, Resources.PurgingActiveFiles);
                        return;
                    }
                    IList<ActiveFile> openFiles = FileSystemState.DecryptedActiveFiles;
                    if (openFiles.Count == 0)
                    {
                        return;
                    }
                    StringBuilder sb = new StringBuilder();
                    foreach (ActiveFile openFile in openFiles)
                    {
                        sb.Append("{0}\n".InvariantFormat(Path.GetFileName(openFile.DecryptedFileInfo.FullName))); //MLHIDE
                    }
                    sb.ToString().ShowWarning();
                });
        }

        private void OpenFilesListView_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            string encryptedPath = OpenFilesListView.SelectedItems[0].SubItems["EncryptedPath"].Text; //MLHIDE
            OpenEncrypted(encryptedPath);
        }

        private void RemoveRecentFileMenuItem_Click(object sender, EventArgs e)
        {
            string encryptedPath = RecentFilesListView.SelectedItems[0].SubItems["EncryptedPath"].Text; //MLHIDE
            FileSystemState.RemoveRecentFile(encryptedPath);
        }

        private void RecentFilesListView_MouseClick(object sender, MouseEventArgs e)
        {
            ShowContextMenu(RecentFilesContextMenu, sender, e);
        }

        private static void ShowContextMenu(ContextMenuStrip contextMenu, object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }
            ListView listView = (ListView)sender;
            contextMenu.Show(listView, e.Location);
        }

        private void CloseOpenFilesMenuItem_Click(object sender, EventArgs e)
        {
            PurgeActiveFiles();
        }

        private void CloseOpenFilesButton_Click(object sender, EventArgs e)
        {
            PurgeActiveFiles();
        }

        private void OpenFilesListView_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }
            ListView recentFiles = (ListView)sender;
            OpenFilesContextMenu.Show(recentFiles, e.Location);
        }

        private void EnterPassphraseMenuItem_Click(object sender, EventArgs e)
        {
            string passphraseText = AskForDecryptPassphrase();
            if (passphraseText == null)
            {
                return;
            }
            Passphrase passphrase = new Passphrase(passphraseText);
            bool keyMatch = FileSystemState.UpdateActiveFileWithKeyIfKeyMatchesThumbprint(passphrase.DerivedPassphrase);
            if (keyMatch)
            {
                FileSystemState.KnownKeys.Add(passphrase.DerivedPassphrase);
            }
        }

        private void progressBackgroundWorker_ProgressBarClicked(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                return;
            }
            ProgressContextMenu.Tag = sender;
            ProgressContextMenu.Show((Control)sender, e.Location);
        }

        private void progressBackgroundWorker_ProgressBarCreated(object sender, ControlEventArgs e)
        {
            ProgressPanel.Controls.Add(e.Control);
        }

        private void ProgressContextCancelMenu_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menuItem = (ToolStripMenuItem)sender;
            ContextMenuStrip menuStrip = (ContextMenuStrip)menuItem.GetCurrentParent();
            ProgressBar progressBar = (ProgressBar)menuStrip.Tag;
            BackgroundWorker worker = (BackgroundWorker)progressBar.Tag;
            worker.CancelAsync();
        }

        private void encryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EncryptFilesViaDialog();
        }

        private void decryptToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DecryptFilesViaDialog();
        }

        private void AxCryptMainForm_Resize(object sender, EventArgs e)
        {
            if (OS.Current.Platform != Platform.WindowsDesktop)
            {
                return;
            }

            TrayNotifyIcon.BalloonTipTitle = Resources.AxCryptFileEncryption;
            TrayNotifyIcon.BalloonTipText = Resources.TrayBalloonTooltip;

            if (FormWindowState.Minimized == this.WindowState)
            {
                TrayNotifyIcon.Visible = true;
                TrayNotifyIcon.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                TrayNotifyIcon.Visible = false;
            }
        }

        private void TrayNotifyIcon_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void EnglishMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("en-US"); //MLHIDE
        }

        private void SwedishMenuItem_Click(object sender, EventArgs e)
        {
            SetLanguage("sv-SE"); //MLHIDE
        }

        private static void SetLanguage(string cultureName)
        {
            Settings.Default.Language = cultureName;
            Settings.Default.Save();
            if (OS.Log.IsInfoEnabled)
            {
                OS.Log.LogInfo("Set new UI language culture to '{0}'.".InvariantFormat(Settings.Default.Language)); //MLHIDE
            }
            Resources.LanguageChangeRestartPrompt.ShowWarning();
        }

        private void LanguageMenuItem_DropDownOpening(object sender, EventArgs e)
        {
            ToolStripMenuItem languageMenu = (ToolStripMenuItem)sender;
            CultureInfo currentUICulture = Thread.CurrentThread.CurrentUICulture;
            if (!currentUICulture.IsNeutralCulture)
            {
                currentUICulture = currentUICulture.Parent;
            }
            string currentLanguage = currentUICulture.Name;
            foreach (ToolStripItem item in languageMenu.DropDownItems)
            {
                string languageName = item.Tag as string;
                if (String.IsNullOrEmpty(languageName))
                {
                    continue;
                }
                if (languageName == currentLanguage)
                {
                    ((ToolStripMenuItem)item).Checked = true;
                    break;
                }
            }
        }

        private void UpdateToolStripButton_Click(object sender, EventArgs e)
        {
            Settings.Default.LastUpdateCheckUtc = OS.Current.UtcNow;
            Settings.Default.Save();
            Process.Start(_updateUrl.ToString());
        }

        private void debugToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Settings.Default.Debug = !Settings.Default.Debug;
            Settings.Default.Save();
            UpdateDebugMode();
        }

        private TabPage _logTabPage = null;

        private void UpdateDebugMode()
        {
            debugToolStripMenuItem1.Checked = Settings.Default.Debug;
            DebugToolStripMenuItem.Visible = Settings.Default.Debug;
            if (Settings.Default.Debug)
            {
                OS.Log.SetLevel(LogLevel.Debug);
                if (_logTabPage != null)
                {
                    StatusTabs.TabPages.Add(_logTabPage);
                }
                ServicePointManager.ServerCertificateValidationCallback = (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return true;
                };
            }
            else
            {
                ServicePointManager.ServerCertificateValidationCallback = null;
                OS.Log.SetLevel(LogLevel.Error);
                _logTabPage = StatusTabs.TabPages["LogTab"];
                StatusTabs.TabPages.Remove(_logTabPage);
            }
        }

        private void setUpdateCheckUrlToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (DebugOptionsDialog dialog = new DebugOptionsDialog())
            {
                dialog.UpdateCheckServiceUrl.Text = Settings.Default.AxCrypt2VersionCheckUrl.ToString();
                DialogResult result = dialog.ShowDialog();
                if (result != DialogResult.OK)
                {
                    return;
                }
                Settings.Default.AxCrypt2VersionCheckUrl = new Uri(dialog.UpdateCheckServiceUrl.Text);
            }
        }

        private void checkVersionNow_Click(object sender, EventArgs e)
        {
            UpdateCheck(DateTime.MinValue);
        }

        private void about_Click(object sender, EventArgs e)
        {
            using (AboutBox aboutBox = new AboutBox())
            {
                aboutBox.ShowDialog();
            }
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.AxCrypt2HelpUrl.ToString());
        }

        private void viewHelpMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Settings.Default.AxCrypt2HelpUrl.ToString());
        }
    }
}