using System;
using System.Collections.ObjectModel;
using Client.Protocol;
using System.Collections.Generic;
using System.Windows.Input;
using System.Linq;

namespace Client.UI
{
	public class StartChatEventArgs : EventArgs
	{
		public StartChatEventArgs(Contact entity)
		{
			Entity = entity;
		}

		public readonly Contact Entity;
	}

	public class NewTabViewModel : BaseViewModel
	{
		private ObservableCollection<ContactViewModel> contactVMs = new ObservableCollection<ContactViewModel>();
		private ChatClient client;

		public event EventHandler<StartChatEventArgs> StartChat;

		public ICommand StartChatCommand
		{
			get
			{
				return new RelayCommand(
					vm => { if (StartChat != null) StartChat(this, new StartChatEventArgs((vm as ContactViewModel).Contact)); }
					);
			}
		}

		public NewTabViewModel(ChatClient client)
		{
			this.client = client;
			client.UserDetailsChange += OnUserDetailsChange;
			client.GroupDetailsChange += OnGroupDetailsChange;
		}

		private void OnUserDetailsChange(object sender, UserDetailsEventArgs e)
		{
			App.Current.Dispatcher.Invoke(() =>
			{
				UpdateContacts(e.AddedUsers, e.ChangedUsers);
			});
		}

		private void OnGroupDetailsChange(object sender, GroupDetailsEventArgs e)
		{
			App.Current.Dispatcher.Invoke(() =>
			{
				UpdateContacts(e.AddedGroups, e.ChangedGroups);
			});
		}
		
		public ReadOnlyObservableCollection<ContactViewModel> Contacts
		{
			get
			{
				return new ReadOnlyObservableCollection<ContactViewModel>(contactVMs);
			}
		}

		public void UpdateContacts(IEnumerable<Contact> addedContacts, IEnumerable<Contact> changedContacts)
		{
			foreach (var contact in addedContacts)
				contactVMs.Add(ContactViewModel.Create(client, contact));

			foreach (var contact in changedContacts)
			{
				var vm = contactVMs.FirstOrDefault(_ => _.Contact.Name == contact.Name);
				if (contact is User)
				{
					if ((contact as User).Relation == UserRelation.None)
						contactVMs.Remove(vm);
				}
				else
				{
					if (!(contact as Group).Joined)
						contactVMs.Remove(vm);
				}
			}
		}
	}
}
