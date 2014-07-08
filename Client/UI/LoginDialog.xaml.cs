using Client.Protocol;
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
	public partial class LoginDialog : MetroWindow, IView
	{
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		Protocol.ChatClient client;
		IViewController views;

		public LoginDialog(IViewController views)
			: this(views, true)
		{ }

		public LoginDialog(IViewController views, bool isFirstRun)
		{
			this.views = views;

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

			var matchingServer = ServerComboBox.Items.Cast<ComboBoxItem>().SingleOrDefault(item => item.Tag as string == serverName);
			if (matchingServer != null)
				ServerComboBox.SelectedItem = matchingServer;
			else
				ServerComboBox.Text = serverName;

			if (isFirstRun && password != "")
			{
				PasswordTextBox.Password = password;
				AutoLoginCheckBox.IsChecked = true;
			}

			Loaded += OnLoaded;
		}

		void OnLoaded(object sender, RoutedEventArgs e)
		{
			if (AutoLoginCheckBox.IsChecked ?? false)
			{
				Hide();
				TryLogIn();
			}
		}

		void LoginButtonClick(object sender, RoutedEventArgs e)
		{
			TryLogIn();
		}

		void TryLogIn()
		{
			string username = UsernameTextBox.Text;
			string password = PasswordTextBox.Password;
			string server;
			if (ServerComboBox.SelectedItem == null)
				server = ServerComboBox.Text;
			else
				server = (ServerComboBox.SelectedItem as ComboBoxItem).Tag as string;

			client = new ChatClient();
			App.ChatClient = client;
			App.ConnectionManager = new ConnectionManager(client);
			if (App.ConnectionManager.Connect(server, ConnectionManager.DefaultPort, username, password))
			{
				var settings = Properties.Settings.Default;
				if (RememberUsernameCheckBox.IsChecked ?? false)
					settings.Username = username;

				settings.Server = server;

				if (AutoLoginCheckBox.IsChecked ?? false)
					settings.Password = password;

				settings.Save();

				var mainView = views.CreateMainView();
				views.Navigate(mainView);
			}
			else
			{
				Show();
			}
		}

		void CancelButtonClick(object sender, RoutedEventArgs e)
		{
			Close();
		}

		object IView.ViewModel
		{
			get { return this; }
		}

		void IView.Show()
		{
			Show();
		}

		bool? IView.ShowModal()
		{
			return ShowDialog();
		}

		void IView.Close()
		{
			Close();
		}
	}
}
