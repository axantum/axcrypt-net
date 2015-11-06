﻿namespace Axantum.AxCrypt
{
    partial class CreateNewAccountDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.PassphraseTextBox = new System.Windows.Forms.TextBox();
            this.PassphraseGroupBox = new System.Windows.Forms.GroupBox();
            this.ShowPassphraseCheckBox = new System.Windows.Forms.CheckBox();
            this.VerifyPassphraseTextbox = new System.Windows.Forms.TextBox();
            this._label1 = new System.Windows.Forms.Label();
            this._panel1 = new System.Windows.Forms.Panel();
            this._buttonCancel = new System.Windows.Forms.Button();
            this._buttonOk = new System.Windows.Forms.Button();
            this.EmailTextBox = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this._errorProvider1 = new System.Windows.Forms.ErrorProvider(this.components);
            this._errorProvider2 = new System.Windows.Forms.ErrorProvider(this.components);
            this._errorProvider3 = new System.Windows.Forms.ErrorProvider(this.components);
            this.PassphraseGroupBox.SuspendLayout();
            this._panel1.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider3)).BeginInit();
            this.SuspendLayout();
            // 
            // PassphraseTextBox
            // 
            this.PassphraseTextBox.CausesValidation = false;
            this.PassphraseTextBox.Location = new System.Drawing.Point(7, 20);
            this.PassphraseTextBox.Name = "PassphraseTextBox";
            this.PassphraseTextBox.Size = new System.Drawing.Size(242, 20);
            this.PassphraseTextBox.TabIndex = 0;
            // 
            // PassphraseGroupBox
            // 
            this.PassphraseGroupBox.AutoSize = true;
            this.PassphraseGroupBox.Controls.Add(this.ShowPassphraseCheckBox);
            this.PassphraseGroupBox.Controls.Add(this.VerifyPassphraseTextbox);
            this.PassphraseGroupBox.Controls.Add(this._label1);
            this.PassphraseGroupBox.Controls.Add(this.PassphraseTextBox);
            this.PassphraseGroupBox.Location = new System.Drawing.Point(2, 47);
            this.PassphraseGroupBox.Name = "PassphraseGroupBox";
            this.PassphraseGroupBox.Size = new System.Drawing.Size(280, 125);
            this.PassphraseGroupBox.TabIndex = 2;
            this.PassphraseGroupBox.TabStop = false;
            this.PassphraseGroupBox.Text = "Enter Password";
            // 
            // ShowPassphraseCheckBox
            // 
            this.ShowPassphraseCheckBox.AutoSize = true;
            this.ShowPassphraseCheckBox.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.ShowPassphraseCheckBox.Location = new System.Drawing.Point(7, 89);
            this.ShowPassphraseCheckBox.Name = "ShowPassphraseCheckBox";
            this.ShowPassphraseCheckBox.Size = new System.Drawing.Size(111, 17);
            this.ShowPassphraseCheckBox.TabIndex = 3;
            this.ShowPassphraseCheckBox.Text = "Show Password";
            this.ShowPassphraseCheckBox.UseVisualStyleBackColor = true;
            // 
            // VerifyPassphraseTextbox
            // 
            this.VerifyPassphraseTextbox.Location = new System.Drawing.Point(6, 59);
            this.VerifyPassphraseTextbox.Name = "VerifyPassphraseTextbox";
            this.VerifyPassphraseTextbox.Size = new System.Drawing.Size(243, 20);
            this.VerifyPassphraseTextbox.TabIndex = 2;
            // 
            // _label1
            // 
            this._label1.AutoSize = true;
            this._label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._label1.Location = new System.Drawing.Point(6, 43);
            this._label1.Name = "_label1";
            this._label1.Size = new System.Drawing.Size(91, 13);
            this._label1.TabIndex = 1;
            this._label1.Text = "Verify Password";
            // 
            // _panel1
            // 
            this._panel1.Controls.Add(this._buttonCancel);
            this._panel1.Controls.Add(this._buttonOk);
            this._panel1.Location = new System.Drawing.Point(42, 177);
            this._panel1.Name = "_panel1";
            this._panel1.Size = new System.Drawing.Size(200, 37);
            this._panel1.TabIndex = 0;
            // 
            // _buttonCancel
            // 
            this._buttonCancel.CausesValidation = false;
            this._buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this._buttonCancel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._buttonCancel.Location = new System.Drawing.Point(103, 11);
            this._buttonCancel.Name = "_buttonCancel";
            this._buttonCancel.Size = new System.Drawing.Size(75, 23);
            this._buttonCancel.TabIndex = 1;
            this._buttonCancel.Text = "Cancel";
            this._buttonCancel.UseVisualStyleBackColor = true;
            // 
            // _buttonOk
            // 
            this._buttonOk.CausesValidation = false;
            this._buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this._buttonOk.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this._buttonOk.Location = new System.Drawing.Point(22, 11);
            this._buttonOk.Name = "_buttonOk";
            this._buttonOk.Size = new System.Drawing.Size(75, 23);
            this._buttonOk.TabIndex = 0;
            this._buttonOk.Text = "OK";
            this._buttonOk.UseVisualStyleBackColor = true;
            this._buttonOk.Click += new System.EventHandler(this._buttonOk_Click);
            // 
            // EmailTextBox
            // 
            this.EmailTextBox.Location = new System.Drawing.Point(9, 18);
            this.EmailTextBox.Name = "EmailTextBox";
            this.EmailTextBox.Size = new System.Drawing.Size(242, 20);
            this.EmailTextBox.TabIndex = 0;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.EmailTextBox);
            this.groupBox1.Location = new System.Drawing.Point(2, 2);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(3, 3, 13, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(280, 44);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Email";
            // 
            // _errorProvider1
            // 
            this._errorProvider1.ContainerControl = this;
            // 
            // _errorProvider2
            // 
            this._errorProvider2.ContainerControl = this;
            // 
            // _errorProvider3
            // 
            this._errorProvider3.ContainerControl = this;
            // 
            // CreateNewAccountDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 237);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.PassphraseGroupBox);
            this.Controls.Add(this._panel1);
            this.Name = "CreateNewAccountDialog";
            this.Text = "Create Account";
            this.Load += new System.EventHandler(this.CreateNewAccountDialog_Load);
            this.PassphraseGroupBox.ResumeLayout(false);
            this.PassphraseGroupBox.PerformLayout();
            this._panel1.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this._errorProvider3)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        internal System.Windows.Forms.TextBox PassphraseTextBox;
        internal System.Windows.Forms.GroupBox PassphraseGroupBox;
        internal System.Windows.Forms.CheckBox ShowPassphraseCheckBox;
        private System.Windows.Forms.TextBox VerifyPassphraseTextbox;
        private System.Windows.Forms.Label _label1;
        private System.Windows.Forms.Panel _panel1;
        private System.Windows.Forms.Button _buttonCancel;
        private System.Windows.Forms.Button _buttonOk;
        internal System.Windows.Forms.TextBox EmailTextBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ErrorProvider _errorProvider1;
        private System.Windows.Forms.ErrorProvider _errorProvider2;
        private System.Windows.Forms.ErrorProvider _errorProvider3;
    }
}