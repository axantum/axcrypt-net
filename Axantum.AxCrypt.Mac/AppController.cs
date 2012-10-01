using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using Axantum.AxCrypt.Core.IO;
using System.IO;
using Axantum.AxCrypt.Core.Crypto;
using Axantum.AxCrypt.Core.UI;
using Axantum.AxCrypt.Core;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using Axantum.AxCrypt.Mono;
using Axantum.AxCrypt.Core.System;

namespace Axantum.AxCrypt.Mac
{
	public class AppController
	{
		const string APP_NAME = "AxCrypt for Mac";
		const string VERSION = "2.0.1.0";

		public static string FullApplicationName {
			get {
				return String.Concat(APP_NAME, ", version ", VERSION);
			}
		}

		public AppController ()
		{
		}

		public static void OnlineHelp ()
		{
			Process.Start("http://www.axantum.com/AxCrypt/Default.html");
		}

		static IRuntimeFileInfo GetTargetFileName (string sourceFilePath, string encryptedFileName)
		{
			if (String.IsNullOrEmpty (encryptedFileName))
				encryptedFileName = DateTime.Now.ToString ("yyyyMMddHHmmss");

			if (!encryptedFileName.EndsWith(Os.Current.AxCryptExtension))
				encryptedFileName += Os.Current.AxCryptExtension;

			return Os.Current.FileInfo(Path.Combine(Path.GetDirectoryName(sourceFilePath), encryptedFileName));
		}

		public static void EncryptFile (ProgressContext progress)
		{
			CreatePassphraseViewController passphraseController = new CreatePassphraseViewController {
				EncryptedFileName = DateTime.Now.ToString("yyyyMMddHHmmss")
			};

			NSOpenPanel open = new NSOpenPanel {
				AccessoryView = passphraseController.View,
				AllowsMultipleSelection = false,
				CanChooseDirectories = false,
				CanChooseFiles = true,
				CanSelectHiddenExtension = true,
				CollectionBehavior = NSWindowCollectionBehavior.Transient,
				ExtensionHidden = true,
				Message = "Please select the file you would like to encrypt",
				Prompt = "Encrypt file",
				Title = "Encrypt",
				TreatsFilePackagesAsDirectories = false,
			};
			
			open.Begin(result => {
				if (result == 0 || open.Urls.Length == 0) return;
				if (!open.Urls[0].IsFileUrl) return;
				string sourceFilePath = open.Urls[0].Path;
				open.Close();

				IRuntimeFileInfo sourceFile = Os.Current.FileInfo(sourceFilePath);
				Passphrase passphrase = passphraseController.VerifiedPassphrase;
				if (passphrase == null) return;

				IRuntimeFileInfo targetFile = GetTargetFileName(sourceFilePath, passphraseController.EncryptedFileName);

				ThreadPool.QueueUserWorkItem(delegate { 
					using(new NSAutoreleasePool()) {
						AxCryptFile.EncryptFileWithBackupAndWipe(sourceFile, targetFile, passphrase.DerivedPassphrase, progress);
					};
				});
			});
		}

		private static void GetSourceFile (Action<IRuntimeFileInfo, Passphrase> fileSelected)
		{
			NSOpenPanel panel = NSOpenPanel.OpenPanel;
			PasswordViewController passwordController = new PasswordViewController();
			panel.AccessoryView = passwordController.View;

			panel.Begin (result => {
				if (result == 0 || panel.Urls.Length == 0) return;
				if (!panel.Urls[0].IsFileUrl) return;
				string filePath = panel.Urls[0].Path;
				panel.Close();
				ThreadPool.QueueUserWorkItem(delegate { 
					using(new NSAutoreleasePool()) {
						fileSelected(Os.Current.FileInfo(filePath), passwordController.Passphrase); 
					};
				});
			});
		}

		static void GetTargetPath (Action<string> directorySelected)
		{
			NSOpenPanel panel = NSOpenPanel.OpenPanel;
			panel.CanChooseFiles = false;
			panel.CanChooseDirectories = true;

			panel.Begin(result => {
				if (result == 0 || panel.Urls.Length == 0) return;
				if (!panel.Urls[0].IsFileUrl) return;
				string filePath = panel.Urls[0].Path;
				panel.Close();

				directorySelected(filePath);
			});
		}

		static bool TryDecrypt (IRuntimeFileInfo file, string filePath, AesKey key, ProgressContext progress, out string encryptedFileName)
		{
			encryptedFileName = AxCryptFile.Decrypt(file, filePath, key, AxCryptOptions.EncryptWithCompression, progress);
			
			if (encryptedFileName == null) {
				progress.DisplayText = "Invalid password: Check your caps lock button and try again";
				progress.Interrupt();
				return false;
			}
			return true;
		}

		public static void DecryptAndOpenFile (ProgressContext progress)
		{
			GetSourceFile((file, passphrase) => {
				string filePath = Path.GetTempPath();
				string fileName;
				AesKey key = passphrase.DerivedPassphrase;

				if (!TryDecrypt(file, filePath, key, progress, out fileName))
					return;

				IRuntimeFileInfo target = Os.Current.FileInfo(Path.Combine(filePath, fileName));

				ILauncher launcher = Os.Current.Launch(target.FullName);
				launcher.Exited += delegate {
					AxCryptFile.EncryptFileWithBackupAndWipe(target, file, key, progress);
					launcher.Dispose();
				};
			});
		}

		public static void DecryptFile(ProgressContext progress) {
			GetSourceFile((file, passphrase) => {

				string targetDirectory = Path.GetDirectoryName(file.FullName);
				string fileName;

				if (!TryDecrypt(file, targetDirectory, passphrase.DerivedPassphrase, progress, out fileName))
					return;
			});
		}

		public static void About(object sender)
		{
			AboutWindowController controller = new AboutWindowController();
			controller.ShowWindow((NSObject)sender);
			controller.SetVersion(VERSION);
		}
	}
}
