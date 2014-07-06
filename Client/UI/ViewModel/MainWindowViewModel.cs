using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Protocol;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.IO;
using System.Net.Sockets;
using System.Drawing;

namespace Client.UI.ViewModel
{
	public class MainWindowViewModel : BaseViewModel
	{
		ListCollectionView openTabsView;
		bool disposed = false;
		ChatClient client;
		ConnectionManager connection;
		ConversationManager conversations;
		UserViewModel me;
		UserStateDetector stateDetector;
		System.Timers.Timer tabHighlightTimer = new System.Timers.Timer(1000);
		List<ConversationViewModel> signalledTabs = new List<ConversationViewModel>();
		bool tabsHighlighted = false;

		public ObservableCollection<BaseViewModel> OpenTabs
		{ get; private set; }

		public ListCollectionView OpenTabsView
		{ get { return openTabsView; } }

		public UserViewModel Me
		{ get { return me; } }

		void SignalTab(ConversationViewModel convoVM)
		{
			if (!signalledTabs.Contains(convoVM))
			{
				if (openTabsView.CurrentItem == convoVM)
					return;

				if (signalledTabs.Count == 0)
					tabHighlightTimer.Start();

				signalledTabs.Add(convoVM);
				convoVM.IsHighlighted = true;
			}
		}

		void UnsignalTab(ConversationViewModel convoVM)
		{
			convoVM.IsHighlighted = false;
			signalledTabs.Remove(convoVM);

			if (signalledTabs.Count == 0)
				tabHighlightTimer.Stop();
		}

		public MainWindowViewModel(ChatClient client, ConnectionManager connection)
		{
			this.client = client;
			this.connection = connection;

			stateDetector = new UserStateDetector();
			stateDetector.IdleTimeThreshold = 60 * 5;
			stateDetector.IsIdleEnabled = true;
			stateDetector.IsBusyEnabled = true;
			stateDetector.UserIdleChanged += (sender, e) =>
			{
				if (client.Me.Status == UserStatus.Available && e.IsIdle)
					client.ChangeStatus(UserStatus.Away, null);
				else if (client.Me.Status == UserStatus.Away && !e.IsIdle)
					client.ChangeStatus(UserStatus.Available, null);
			};
			stateDetector.UserBusyChanged += (sender, e) =>
			{
				if (client.Me.Status == UserStatus.Available && e.IsBusy)
					client.ChangeStatus(UserStatus.Busy, null);
				else if (client.Me.Status == UserStatus.Busy && !e.IsBusy)
					client.ChangeStatus(UserStatus.Available, null);
			};

			client.StreamError += OnStreamError;
			me = new UserViewModel(client, client.Me);
			conversations = new ConversationManager(client);
			conversations.NewConversation += OnNewConversation;

			OpenTabs = new ObservableCollection<BaseViewModel>(new List<BaseViewModel>());
			openTabsView = new ListCollectionView(OpenTabs);
			openTabsView.CurrentChanged += (sender, e) =>
				{
					var convoVM = openTabsView.CurrentItem as ConversationViewModel;
					if (convoVM != null)
					{
						UnsignalTab(convoVM);
					}
				};

			var newTabVM = new NewTabViewModel(client);
			newTabVM.StartChat += (sender, e) =>
				{
					var convo = conversations.CreateConversation(e.Contact);
					SwitchToConversation(convo);
				};
			OpenTabs.Add(newTabVM);

			tabHighlightTimer.Elapsed += OnTabHighlight;

			client.ListFriends();
			client.ListGroups();
		}

		void OnTabHighlight(object sender, System.Timers.ElapsedEventArgs e)
		{
			App.Current.Dispatcher.Invoke(() =>
				{
					tabsHighlighted = !tabsHighlighted;
					foreach (var tab in signalledTabs)
					{
						tab.IsHighlighted = tabsHighlighted;
					}
				});
		}

		void OnNewConversation(object sender, NewConversationEventArgs e)
		{
			App.Current.Dispatcher.Invoke(() =>
			{
				var convoVM = new ConversationViewModel(client, e.Conversation);
				OpenTabs.Add(convoVM);
				e.Conversation.ChatReceived += (_sender, _e) =>
					{
						SignalTab(convoVM);
					};
				openTabsView.Refresh();
				SignalTab(convoVM);
			});
		}

		void OnStreamError(object sender, StreamErrorEventArgs e)
		{
			App.Current.Dispatcher.Invoke(() =>
				{
					bool handled = false;

					if (e.Exception is IOException)
					{
						var socketEx = e.Exception.InnerException as SocketException;
						if (socketEx != null)
						{
							switch (socketEx.SocketErrorCode)
							{
								case SocketError.Interrupted:
									handled = true;
									break;

								case SocketError.ConnectionAborted:
								case SocketError.ConnectionReset:
									if (!connection.Reconnect())
									{
										// Please fix this garbage
										LogoutCommand.Execute(null);
									}
									handled = true;
									break;
							}
						}
					}

					if (!handled)
						throw e.Exception;
				});
		}

		public void SwitchToConversation(Conversation convo)
		{
			var convoVM = OpenTabs.Single(vm =>
				{
					if (vm is ConversationViewModel)
						return (vm as ConversationViewModel).Conversation == convo;
					return false;
				}) as ConversationViewModel;

			OpenTabsView.MoveCurrentTo(convoVM);
		}

		public ICommand ChangeNameCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					client.ChangeDisplayName(_ as string, null);
				});
			}
		}

		public ICommand ChangeFontCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					var fontDialog = new FontDialog(App.Current.ClientFont);
					if (fontDialog.ShowDialog() ?? false)
					{
						App.Current.ClientFont = fontDialog.SelectedFont;
					}
				});
			}
		}

		public ICommand ChangeStatusCommand
		{
			get
			{
				return new RelayCommand(rawStatus =>
				{
					UserStatus status;
					if (Enum.TryParse(rawStatus as string, out status))
						client.ChangeStatus(status, null);
				}, rawStatus =>
				{
					UserStatus status;
					return Enum.TryParse(rawStatus as string, out status);
				});
			}
		}

		public ICommand CloseChatCommand
		{
			get
			{
				return new RelayCommand(rawVM =>
				{
					conversations.DeleteConversation((rawVM as ConversationViewModel).Conversation);
					OpenTabs.Remove(rawVM as BaseViewModel);
				});
			}
		}

		public ICommand LogoutCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					// I need a shower after this code
					var mainWindow = App.Current.MainWindow;
					var loginDialog = new LoginDialog(false);
					App.Current.MainWindow = loginDialog;
					loginDialog.Show();
					mainWindow.Close();
				});
			}
		}

		public ICommand QuitCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					App.Current.Shutdown();
				});
			}
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				client.Disconnect();
				client.Dispose();

				stateDetector.Dispose();
			}
		}
	}
}
