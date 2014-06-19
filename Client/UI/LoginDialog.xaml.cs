using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.IO;
using System.Net.Sockets;
using System.Threading;

using MahApps.Metro.Controls;
using Newtonsoft.Json.Linq;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for LoginDialog.xaml
	/// </summary>
	public partial class LoginDialog : MetroWindow
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private Protocol.ChatClient clientHandler;

		public LoginDialog()
		{
			Initialize(true);
		}

		public LoginDialog(bool isFirstRun)
		{
			Initialize(isFirstRun);
		}

		private void Initialize(bool isFirstRun)
		{
			InitializeComponent();

			string username = Properties.Settings.Default.Username;
			string passwordHash = Properties.Settings.Default.Password;
			string server = Properties.Settings.Default.Server;

			if (username != "")
			{
				UsernameTextBox.Text = username;
				PasswordTextBox.Focus();
			}
			else
				UsernameTextBox.Focus();

			ServerComboBox.Text = server;

			if (isFirstRun && passwordHash != "")
			{
				Hide();
			}
		}

		private void LoginButtonClick(object sender, RoutedEventArgs e)
		{
			IsEnabled = false;

			string username = UsernameTextBox.Text;
			string password = PasswordTextBox.Password;
			string server = ServerComboBox.Text;

			new Thread(() =>
				{
					var netClient = new TcpClient(server, 49520);
					clientHandler = new Protocol.ChatClient();
					clientHandler.Connect(netClient.GetStream());
					clientHandler.LogIn(username, password, OnLoginReply);
				}).Start();
		}

		private void OnLoginReply(object sender, Protocol.LoginEventArgs e)
		{
			if (e.Success)
			{
				try
				{
					Dispatcher.Invoke(new Action(() =>
						{
							App.Current.MainWindow = new MainWindow(clientHandler);
							App.Current.MainWindow.Show();
							Close();
						}));
				}
				catch (Exception)
				{
					System.Diagnostics.Debugger.Break();
				}
			}
			else
			{
				Dispatcher.Invoke(() => { IsEnabled = true; });
			}
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
