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
using System.Windows.Media;
using System.Windows.Threading;

namespace Client.UI.ViewModel
{
	public class MainWindowViewModel : BaseViewModel
	{
		ConnectionManager connection;
		ChatClient client;
		UserViewModel me;
		ConversationManager conversations;
		Dispatcher dispatcher;
		IViewController views;

		ObservableCollection<BaseViewModel> openTabs = new ObservableCollection<BaseViewModel>();
		ListCollectionView openTabsView;

		// Detects and notifies us whenever the user goes away or busy
		UserStateDetector stateDetector;

		// All tabs highlight with the same rhythm
		System.Timers.Timer tabHighlightTimer = new System.Timers.Timer(1000);
		// List of signalled tabs (tabs that will have their highlights set)
		List<ConversationViewModel> signalledTabs = new List<ConversationViewModel>();
		// Current highlight state of signalled tabs
		bool tabsHighlighted = false;
		// Status before client is automatically set to idle/busy.  Null if there is none.
		UserStatus? oldStatus;

		bool disposed = false;

		public ListCollectionView OpenTabsView
		{ get { return openTabsView; } }

		public UserViewModel Me
		{ get { return me; } }

		#region Commands
		public ICommand ChangeNameCommand
		{ get; private set; }

		public ICommand ChangeFontCommand
		{ get; private set; }

		public ICommand ChangeStatusCommand
		{ get; private set; }

		public ICommand CloseChatCommand
		{ get; private set; }

		public ICommand LogoutCommand
		{ get; private set; }

		public ICommand QuitCommand
		{ get; private set; }

		public ICommand ViewEmotesCommand
		{ get; private set; }
		#endregion

		public MainWindowViewModel(IViewController views, ChatClient client, ConnectionManager connection, Dispatcher dispatcher)
		{
			this.views = views;
			this.client = client;
			this.connection = connection;
			this.dispatcher = dispatcher;

			ChangeNameCommand = new RelayCommand(_ => ChangeName());
			ChangeFontCommand = new RelayCommand(_ => ChangeFont());
			ChangeStatusCommand = new RelayCommand(_ => ChangeStatus(_ as string), _ => CanChangeStatus(_ as string));
			CloseChatCommand = new RelayCommand(_ => CloseConversation(_ as ConversationViewModel));
			LogoutCommand = new RelayCommand(_ => LogOut());
			QuitCommand = new RelayCommand(_ => Quit());
			ViewEmotesCommand = new RelayCommand(_ => ViewEmotes());

			stateDetector = new UserStateDetector();
			stateDetector.IdleTimeThreshold = 60 * 5; // 5 minutes
			stateDetector.IsIdleEnabled = true;
			stateDetector.IsBusyEnabled = true;
			stateDetector.UserIdleChanged += OnUserIdleChanged;
			stateDetector.UserBusyChanged += OnUserBusyChanged;

			client.StreamError += OnStreamError;
			me = new UserViewModel(client, client.Me);
			conversations = new ConversationManager(client);
			conversations.NewConversation += OnNewConversation;

			openTabsView = new ListCollectionView(openTabs);
			openTabsView.CurrentChanged += OnCurrentTabChanged;

			var newTabVM = new NewTabViewModel(client);
			newTabVM.StartChat += OnStartChat;
			openTabs.Add(newTabVM);

			tabHighlightTimer.Elapsed += OnTabHighlight;

			client.ListFriends();
			client.ListGroups();

			var temp = Newtonsoft.Json.Linq.JObject.Parse("{\"message\":\"detail.groups\",\"groups\":[{\"groupname\":\"cocaff\",\"display_name\":\"PERU #1 :D\",\"members\":[\"mmbob\",\"crash\"],\"member\":true}]}");
			client.SpoofReceieveMessage(temp);
		}

		void OnCurrentTabChanged(object sender, EventArgs e)
		{
			var convoVM = openTabsView.CurrentItem as ConversationViewModel;
			if (convoVM != null)
				UnsignalTab(convoVM);
		}

		void OnStartChat(object sender, StartChatEventArgs e)
		{
			var convo = conversations.CreateConversation(e.Contact);
			SwitchToConversation(convo);
		}

		void OnUserIdleChanged(object sender, UserIdleEventArgs e)
		{
			if (e.IsIdle)
			{
				if (client.Me.Status == UserStatus.Available)
				{
					oldStatus = client.Me.Status;
					client.ChangeStatus(UserStatus.Away, null);
				}
			}
			else
			{
				if (oldStatus.HasValue)
					client.ChangeStatus(oldStatus.Value, null);
				oldStatus = null;
			}
		}

		void OnUserBusyChanged(object sender, UserBusyEventArgs e)
		{
			if (e.IsBusy)
			{
				if (client.Me.Status == UserStatus.Available)
				{
					oldStatus = client.Me.Status;
					client.ChangeStatus(UserStatus.Busy, null);
				}
			}
			else
			{
				if (oldStatus.HasValue)
					client.ChangeStatus(oldStatus.Value, null);
				oldStatus = null;
			}
		}

		void OnTabHighlight(object sender, System.Timers.ElapsedEventArgs e)
		{
			dispatcher.Invoke(() =>
				{
					tabsHighlighted = !tabsHighlighted;
					foreach (var tab in signalledTabs)
						tab.IsHighlighted = tabsHighlighted;
				});
		}

		void OnNewConversation(object sender, NewConversationEventArgs e)
		{
			dispatcher.Invoke(() =>
			{
				var convoVM = new ConversationViewModel(client, e.Conversation);
				convoVM.CloseRequested += OnConversationCloseRequested;
				openTabs.Add(convoVM);
				e.Conversation.ChatReceived += (_sender, _e) =>
					{
						SignalTab(convoVM);
					};
				openTabsView.Refresh();
				SignalTab(convoVM);

				if (!e.ClientStarted)
				{
					App.NotificationManager.CreateNotification(e.Conversation.Contact.DisplayName, "has started a conversation", Colors.Transparent, App.NotificationManager.DefaultShowTime);
				}
			});
		}

		void OnConversationCloseRequested(object sender, EventArgs e)
		{
			var convoVM = sender as ConversationViewModel;
			if (convoVM != null)
				CloseConversation(convoVM);
		}

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

		void OnStreamError(object sender, StreamErrorEventArgs e)
		{
			dispatcher.Invoke(() =>
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
										LogOut();
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
			var convoVM = openTabs.Single(vm =>
				{
					if (vm is ConversationViewModel)
						return (vm as ConversationViewModel).Conversation == convo;
					return false;
				}) as ConversationViewModel;

			OpenTabsView.MoveCurrentTo(convoVM);
		}

		void ChangeName()
		{
			throw new NotImplementedException();
		}

		void ChangeFont()
		{
			var fontView = views.CreateFontView();
			if (fontView.ShowModal() ?? false)
			{
				// This is not ideal but the entire app is not MVVM, so there's really no other option.
				App.Current.ClientFont = (fontView.ViewModel as FontDialog).SelectedFont;
			}
		}

		void ChangeStatus(string rawStatus)
		{
			UserStatus status;
			if (Enum.TryParse(rawStatus as string, true, out status))
				client.ChangeStatus(status, null);
		}

		bool CanChangeStatus(string rawStatus)
		{
			return Enum.GetNames(typeof(UserStatus)).Contains(rawStatus, StringComparer.OrdinalIgnoreCase);
		}

		void CloseConversation(ConversationViewModel vm)
		{
			conversations.DeleteConversation(vm.Conversation);
			openTabs.Remove(vm);
			vm.Dispose();
		}

		void LogOut()
		{
			views.Navigate(views.CreateLoginView());
		}

		void Quit()
		{
			App.Current.Shutdown();
		}

		void ViewEmotes()
		{
			views.CreateEmoteView().ShowModal();
		}

		~MainWindowViewModel()
		{
			Dispose(false);
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
