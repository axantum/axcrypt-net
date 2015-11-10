﻿using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.Crypto.Asymmetric;
using Axantum.AxCrypt.Core.Session;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core.UI.ViewModel;
using Axantum.AxCrypt.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Axantum.AxCrypt
{
    public partial class KeyShareDialog : StyledMessageBase
    {
        private SharingListViewModel _viewModel;

        public IEnumerable<UserPublicKey> SharedWith { get; private set; }

        public KeyShareDialog()
        {
            InitializeComponent();
        }

        public KeyShareDialog(Form parent, Func<KnownPublicKeys> knownPublicKeysFactory, IEnumerable<UserPublicKey> sharedWith, LogOnIdentity logOnIdentity)
            : this()
        {
            InitializeStyle(parent);

            _viewModel = new SharingListViewModel(knownPublicKeysFactory, sharedWith, logOnIdentity);
            _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.SharedWith), (aks) => { _sharedWith.Items.Clear(); _sharedWith.Items.AddRange(aks.ToArray()); });
            _viewModel.BindPropertyChanged<IEnumerable<UserPublicKey>>(nameof(SharingListViewModel.NotSharedWith), (aks) => { _notSharedWith.Items.Clear(); _notSharedWith.Items.AddRange(aks.ToArray()); });
            _viewModel.BindPropertyChanged<string>(nameof(SharingListViewModel.NewKeyShare), (email) => SetShareButtonState());

            _sharedWith.SelectedIndexChanged += (sender, e) => SetUnshareButtonState();
            _notSharedWith.SelectedIndexChanged += (sender, e) => SetShareButtonState();
            _newContact.TextChanged += (sender, e) =>
            {
                _viewModel.NewKeyShare = _newContact.Text;
            };

            _shareButton.Click += async (sender, e) => await ShareAsync();
            _shareButton.Click += (sender, e) => SetShareButtonState();
            _unshareButton.Click += (sender, e) => _viewModel.RemoveKeyShares.Execute(_sharedWith.SelectedIndices.Cast<int>().Select(i => (UserPublicKey)_sharedWith.Items[i]));
            _unshareButton.Click += (sender, e) => SetUnshareButtonState();

            SetShareButtonState();
            SetUnshareButtonState();

            _notSharedWith.Focus();
        }

        private async Task ShareAsync()
        {
            await _viewModel.AsyncAddKeyShares.ExecuteAsync(_notSharedWith.SelectedIndices.Cast<int>().Select(i => EmailAddress.Parse(_notSharedWith.Items[i].ToString())));
            if (String.IsNullOrEmpty(_viewModel.NewKeyShare))
            {
                return;
            }
            if (!AdHocValidationDueToMonoLimitations())
            {
                return;
            }
            await _viewModel.AsyncAddNewKeyShare.ExecuteAsync(_viewModel.NewKeyShare);
            _newContact.Text = String.Empty;
        }

        private void SetShareButtonState()
        {
            bool isNewKeyShare = !String.IsNullOrEmpty(_viewModel.NewKeyShare);
            if (isNewKeyShare)
            {
                _notSharedWith.ClearSelected();
                _sharedWith.ClearSelected();
            }
            _shareButton.Visible = _notSharedWith.SelectedIndices.Count > 0 || isNewKeyShare;
            if (_shareButton.Visible)
            {
                _sharedWith.ClearSelected();
            }
        }

        private void SetUnshareButtonState()
        {
            _unshareButton.Visible = _sharedWith.SelectedIndices.Count > 0;
            if (_unshareButton.Visible)
            {
                _notSharedWith.ClearSelected();
            }
        }

        private void _okButton_Click(object sender, EventArgs e)
        {
            SharedWith = _viewModel.SharedWith;
        }

        private bool AdHocValidationDueToMonoLimitations()
        {
            bool validated = AdHocValidateAllFieldsIndependently();
            return validated;
        }

        private bool AdHocValidateAllFieldsIndependently()
        {
            return AdHocValidateNewKeyShare();
        }

        private bool AdHocValidateNewKeyShare()
        {
            _errorProvider1.Clear();
            if (_viewModel[nameof(SharingListViewModel.NewKeyShare)].Length > 0)
            {
                _errorProvider1.SetError(_newContact, Resources.InvalidEmail);
                _errorProvider1.SetIconPadding(_newContact, 3);
                return false;
            }
            return true;
        }
    }
}