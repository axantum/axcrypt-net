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

using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core.UI.ViewModel;
using Axantum.AxCrypt.Forms;
using System;
using System.Linq;
using System.Windows.Forms;
using static Axantum.AxCrypt.Abstractions.TypeResolve;
using Texts = AxCrypt.Content.Texts;

namespace Axantum.AxCrypt.Forms
{
    public partial class NewPassphraseDialog : StyledMessageBase
    {
        private NewPasswordViewModel _viewModel;

        private ToolTip _toolTip = new ToolTip();

        public NewPassphraseDialog()
        {
            InitializeComponent();
        }

        public NewPassphraseDialog(Form parent, string title, string passphrase, string encryptedFileFullName)
            : this()
        {
            InitializeStyle(parent);

            Text = title;
            _viewModel = new NewPasswordViewModel(passphrase, encryptedFileFullName);
            PassphraseTextBox.TextChanged += (sender, e) => { _viewModel.PasswordText = PassphraseTextBox.Text; ClearErrorProviders(); };
            PassphraseTextBox.TextChanged += async (sender, e) => { await _passwordStrengthMeter.MeterAsync(PassphraseTextBox.Text); };
            VerifyPassphraseTextbox.TextChanged += (sender, e) => { _viewModel.Verification = VerifyPassphraseTextbox.Text; ClearErrorProviders(); };
            ShowPassphraseCheckBox.CheckedChanged += (sender, e) => { _viewModel.ShowPassword = ShowPassphraseCheckBox.Checked; };
        }

        protected override void InitializeContentResources()
        {
            Text = Texts.DialogNewPasswordTitle;

            PassphraseGroupBox.Text = Texts.PassphrasePrompt;
            ShowPassphraseCheckBox.Text = Texts.ShowPasswordOptionPrompt;

            _fileGroupBox.Text = Texts.PromptFileText;
            _buttonCancel.Text = "&" + Texts.ButtonCancelText;
            _buttonHelp.Text = "&" + Texts.ButtonHelpText;
            _buttonOk.Text = "&" + Texts.ButtonOkText;
            _buttonOk.Enabled = false;
            _verifyPasswordLabel.Text = Texts.VerifyPasswordPrompt;
        }

        private void EncryptPassphraseDialog_Load(object s, EventArgs ee)
        {
            if (DesignMode)
            {
                return;
            }

            _passwordStrengthMeter.MeterChanged += (sender, e) =>
            {
                _buttonOk.Enabled = _passwordStrengthMeter.IsAcceptable;
                _toolTip.SetToolTip(PassphraseTextBox, _passwordStrengthMeter.StrengthTip);
            };

            _viewModel.BindPropertyChanged(nameof(NewPasswordViewModel.ShowPassword), (bool show) => { PassphraseTextBox.UseSystemPasswordChar = VerifyPassphraseTextbox.UseSystemPasswordChar = !show; });
            _viewModel.BindPropertyChanged(nameof(NewPasswordViewModel.FileName), (string fileName) => { FileNameTextBox.Text = fileName; FileNamePanel.Visible = !String.IsNullOrEmpty(fileName); });
            _viewModel.BindPropertyChanged(nameof(NewPasswordViewModel.PasswordText), (string p) => { PassphraseTextBox.Text = p; });
            _viewModel.BindPropertyChanged(nameof(NewPasswordViewModel.Verification), (string p) => { VerifyPassphraseTextbox.Text = p; });

            PassphraseTextBox.Focus();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (!AdHocValidationDueToMonoLimitations())
            {
                DialogResult = DialogResult.None;
            }
        }

        private bool AdHocValidationDueToMonoLimitations()
        {
            bool validated = true;
            if (_viewModel[nameof(NewPasswordViewModel.Verification)].Length > 0)
            {
                _errorProvider1.SetError(VerifyPassphraseTextbox, Texts.PassphraseVerificationMismatch);
                validated = false;
            }
            if (_viewModel[nameof(NewPasswordViewModel.PasswordText)].Length > 0)
            {
                _errorProvider1.SetError(PassphraseTextBox, Texts.WrongPassphrase);
                validated = false;
            }
            if (validated)
            {
                _errorProvider1.Clear();
            }
            _errorProvider2.Clear();
            return validated;
        }

        private async void _buttonHelp_Click(object sender, EventArgs e)
        {
            await New<IPopup>().ShowAsync(PopupButtons.Ok, Texts.DialogVerifyAccountTitle, Texts.PasswordRulesInfo);
        }

        private void ClearErrorProviders()
        {
            _errorProvider1.Clear();
            _errorProvider2.Clear();
        }
    }
}