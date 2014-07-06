using Client.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace Client.UI.ViewModel
{
	public class NewTabViewModel : BaseViewModel
	{
		ListCollectionView contactsView;
		ObservableCollection<ContactViewModel> contactVMs = new ObservableCollection<ContactViewModel>();
		ChatClient client;

		public event EventHandler<StartChatEventArgs> StartChat;

		public ICommand StartChatCommand
		{
			get
			{
				return new RelayCommand(_ =>
				{
					var handler = StartChat;
					if (handler != null)
						handler(this, new StartChatEventArgs((contactsView.CurrentItem as ContactViewModel).Contact));
				});
			}
		}

		public NewTabViewModel(ChatClient client)
		{
			this.client = client;

			contactsView = new ListCollectionView(contactVMs);
			client.UserDetailsChange += OnUserDetailsChange;
			client.GroupDetailsChange += OnGroupDetailsChange;

			UpdateContacts(client.Friends, Enumerable.Empty<IUser>());
			UpdateContacts(client.Groups, Enumerable.Empty<IGroup>());
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

		public ListCollectionView ContactsView
		{ get { return contactsView; } }

		public void UpdateContacts(IEnumerable<IContact> addedContacts, IEnumerable<IContact> changedContacts)
		{
			foreach (var contact in addedContacts)
				contactVMs.Add(ContactViewModel.Create(client, contact));

			foreach (var contact in changedContacts)
			{
				// Don't include ourself in any lists
				if (client.Me == contact)
					continue;

				var vm = contactVMs.Single(_ => _.Contact.Name == contact.Name);
				if (contact is IUser)
				{
					if ((contact as IUser).Relation == UserRelation.None)
						contactVMs.Remove(vm);
				}
				else
				{
					if (!(contact as IGroup).Joined)
						contactVMs.Remove(vm);
				}
			}

			contactsView.Refresh();

			NotifyPropertyChanged("Contacts");
			NotifyPropertyChanged("ContactsView");
		}
	}

	public class StartChatEventArgs : EventArgs
	{
		public StartChatEventArgs(IContact entity)
		{
			Contact = entity;
		}

		public readonly IContact Contact;
	}
}
