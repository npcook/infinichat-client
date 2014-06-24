using Client.Protocol;
using Client.UI.View;
using Client.UI.ViewModel;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : MetroWindow
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public ChatClient client;
		private FontOptions clientFont;
		private Dictionary<IContact, ChatView> openChats = new Dictionary<IContact, ChatView>();
		private NewTabViewModel newTabViewModel;

		public MainWindow(ChatClient client)
		{
			this.client = client;
			clientFont = App.Current.ClientFont;

			InitializeComponent();
		}

		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed(e);

			client.Disconnect();
		}

		protected override void OnInitialized(EventArgs e)
		{
			base.OnInitialized(e);

			newTabViewModel = new NewTabViewModel(client);
			NewTabPage.DataContext = newTabViewModel;
			newTabViewModel.StartChat += NewTabStartChat;

			client.UserChat += OnChat;
			client.GroupChat += OnChat;
			client.UserDetailsChange += OnUsersChange;
			client.GroupDetailsChange += OnGroupsChange;

			OnClientChange();
		}

		private void OnClientChange()
		{
			(ClientNameButton.Content as TextBlock).Text = client.Me.DisplayName;
			(ClientStatusButton.Content as TextBlock).Text = client.Me.Status.ToString();
			ClientStatusButton.Foreground = App.GetUserStatusBrush(client.Me.Status);
			ClientStatusBorder.Background = App.GetUserStatusBrush(client.Me.Status);
		}

		private void OnUsersChange(object sender, UserDetailsEventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				if (e.ChangedUsers.Contains(client.Me))
					OnClientChange();
			});
		}

		private void OnGroupsChange(object sender, GroupDetailsEventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
			});
		}

		private void SwitchToChatPage(IContact entity)
		{
			ChatView chat;
			if (!openChats.TryGetValue(entity, out chat))
				throw new ArgumentOutOfRangeException("entity");
			foreach (var tab in ChatTabs.Items)
			{
				if (tab is ChatTabItem)
				{
					if (((tab as ChatTabItem).Content as ChatView) == chat)
					{
						ChatTabs.SelectedItem = tab;
						break;
					}
				}
			}
		}

		private ChatView CreateChatPage(IContact entity)
		{
			var chat = new ChatView();
			var chatVM = new ChatViewModel(client, entity);
			chat.DataContext = chatVM;
			chat.Margin = new Thickness(0);
			openChats.Add(entity, chat);

			ChatTabs.Items.Add(new ChatTabItem()
			{
				Header = entity.DisplayName ?? entity.Name,
				Content = chat,
			});

			return chat;
		}

		private void OnChat(object sender, ChatEventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				// TODO: This probably won't work when the user is not in client handler's user cache (reference comparison)
				ChatView chat;
				if (!openChats.TryGetValue(e.User, out chat))
				{
					chat = CreateChatPage(e.User);
				}
				(chat.DataContext as ChatViewModel).AddChatMessage(e.User.DisplayName, e.Body, e.Font, DateTime.UtcNow);
			});
		}

		private void NewTabStartChat(object sender, StartChatEventArgs e)
		{
			Dispatcher.Invoke(() =>
				{
					ChatView chat;
					if (!openChats.TryGetValue(e.Contact, out chat))
						chat = CreateChatPage(e.Contact);
					SwitchToChatPage(e.Contact);
				});
		}

		private void ChatTabsSelectionChanged(object sender, SelectionChangedEventArgs e)
		{

		}

		private void ChangeNameClick(object sender, RoutedEventArgs e)
		{
		}

		private void ChangeFontClick(object sender, RoutedEventArgs e)
		{
			var fontDialog = new FontDialog(clientFont);
			if (fontDialog.ShowDialog() ?? false)
			{
				clientFont = fontDialog.SelectedFont;
				App.Current.ClientFont = clientFont;
			}
		}

		private void LogoutClick(object sender, RoutedEventArgs e)
		{

		}

		private void QuitClick(object sender, RoutedEventArgs e)
		{

		}

		private void ChangeStatus(UserStatus status)
		{
			client.ChangeStatus(status, (sender, e) =>
				{
					if (e.Success)
					{
						(ClientStatusButton.Content as TextBlock).Text = status.ToString();
						ClientStatusButton.Foreground = App.GetUserStatusBrush(status);
						ClientStatusBorder.Background = App.GetUserStatusBrush(status);
					}
				});
		}

		private void AvailableClick(object sender, RoutedEventArgs e)
		{
			ChangeStatus(UserStatus.Available);
		}

		private void AwayClick(object sender, RoutedEventArgs e)
		{
			ChangeStatus(UserStatus.Away);
		}

		private void BusyClick(object sender, RoutedEventArgs e)
		{
			ChangeStatus(UserStatus.Busy);
		}

		private void OfflineClick(object sender, RoutedEventArgs e)
		{
			ChangeStatus(UserStatus.Offline);
		}
	}
}
