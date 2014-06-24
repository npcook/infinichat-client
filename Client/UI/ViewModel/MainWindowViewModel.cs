using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Client.Protocol;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Client.UI.ViewModel
{
	public class MainWindowViewModel : BaseViewModel
	{
		bool disposed = false;
		ChatClient client;
		ConversationManager conversations;
		UserViewModel me;

		public ObservableCollection<BaseViewModel> OpenTabs
		{ get; private set; }

		public UserViewModel Me
		{ get { return me; } }

		public MainWindowViewModel(ChatClient client)
		{
			this.client = client;
			me = new UserViewModel(client, client.Me);
			conversations = new ConversationManager(client);

			OpenTabs = new ObservableCollection<BaseViewModel>(new List<BaseViewModel>());

			var newTabVM = new NewTabViewModel(client);
			newTabVM.StartChat += (sender, e) =>
				{
					OpenTabs.Add(new ConversationViewModel(client, conversations.CreateConversation(e.Contact)));
				};
			OpenTabs.Add(newTabVM);
		}

		public ICommand ChangeNameCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					client.ChangeDisplayName(_ as string);
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
				client = null;
			}
		}
	}
}
