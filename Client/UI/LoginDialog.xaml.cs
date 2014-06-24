using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for LoginDialog.xaml
	/// </summary>
	public partial class LoginDialog : MetroWindow
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private Protocol.ChatClient client;

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

			var settings = Properties.Settings.Default;
			string username = settings.Username;
			string password = settings.Password;
			string serverName = settings.Server;

			if (username != "")
			{
				UsernameTextBox.Text = username;
				RememberUsernameCheckBox.IsChecked = true;
				PasswordTextBox.Focus();
			}
			else
				UsernameTextBox.Focus();

			var matchingServer = from ComboBoxItem serverItem in ServerComboBox.Items where serverItem.Tag as string == serverName select serverItem;

			ServerComboBox.Text = serverName;
			foreach (var server in matchingServer)
				ServerComboBox.SelectedItem = server;

			if (isFirstRun && password != "")
			{
				Hide();

				PasswordTextBox.Password = password;
				TryLogIn();
			}
		}

		private void LoginButtonClick(object sender, RoutedEventArgs e)
		{
			TryLogIn();
		}

		private void TryLogIn()
		{
			MainGrid.IsEnabled = false;

			string username = UsernameTextBox.Text;
			string password = PasswordTextBox.Password;
			string server;
			if (ServerComboBox.SelectedItem == null)
				server = ServerComboBox.Text;
			else
				server = (ServerComboBox.SelectedItem as ComboBoxItem).Tag as string;

			new Thread(() =>
			{
				try
				{
					var netClient = new TcpClient(server, 49520);
					client = new Protocol.ChatClient();
					client.Connect(netClient.GetStream());
					client.LogIn(username, password, OnLoginReply);
				}
				catch (SocketException ex)
				{
					Dispatcher.Invoke(() =>
					{
						MainGrid.IsEnabled = true;

						MessageBox.Show("Could not log in.", ex.Message);
					});
				}
			}).Start();
		}

		private void OnLoginReply(object sender, Protocol.LoginEventArgs e)
		{
			if (e.Success)
			{
				Dispatcher.Invoke(new Action(() =>
					{
						var settings = Properties.Settings.Default;
						if (RememberUsernameCheckBox.IsChecked ?? false)
							settings.Username = UsernameTextBox.Text;

						if (ServerComboBox.SelectedItem == null)
							settings.Server = ServerComboBox.Text;
						else
							settings.Server = (ServerComboBox.SelectedItem as ComboBoxItem).Tag as string;

						if (AutoLoginCheckBox.IsChecked ?? false)
							settings.Password = PasswordTextBox.Password;

						settings.Save();

						App.Current.MainWindow = new View.MainWindowView();
						App.Current.MainWindow.DataContext = new ViewModel.MainWindowViewModel(client);
						App.Current.MainWindow.Show();
//						App.Current.MainWindow = new MainWindow(client);
//						App.Current.MainWindow.Show();
						Close();
					}));
			}
			else
			{
				Dispatcher.Invoke(() => { MainGrid.IsEnabled = true; });
			}
		}

		private void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}
	}
}
