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
		string addFriendText = "";

		public ICommand StartChatCommand
		{ get; private set; }

		public ICommand AddFriendCommand
		{ get; private set; }

		public NewTabViewModel(ChatClient client)
		{
			this.client = client;

			StartChatCommand = new RelayCommand(_ => 
				{
					StartChat.SafeInvoke(this, new StartChatEventArgs((contactsView.CurrentItem as ContactViewModel).Contact));
				});
			AddFriendCommand = new RelayCommand(_ =>
				{
					if (AddFriendText.Length > 0)
						client.AddFriend(AddFriendText);
					AddFriendText = "";
				}, _ =>
				{
					return AddFriendText.Length > 0;
				});

			contactsView = new ListCollectionView(contactVMs);
			contactsView.CustomSort = new ContactComparer();

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
		{ get { return new ReadOnlyObservableCollection<ContactViewModel>(contactVMs); } }

		public ListCollectionView ContactsView
		{ get { return contactsView; } }

		public string AddFriendText
		{
			get { return addFriendText; }
			set
			{
				addFriendText = value;
				NotifyPropertyChanged("AddFriendText");
			}
		}

		public void UpdateContacts(IEnumerable<IContact> addedContacts, IEnumerable<IContact> changedContacts)
		{
			foreach (var contact in addedContacts)
			{
				bool add = false;
				var user = contact as IUser;
				var group = contact as IGroup;

				if (user != null && (user.Relation == UserRelation.Friend || user.Relation == UserRelation.PendingFriend))
					add = true;
				else if (group != null && group.Joined)
					add = true;
				if (add)
					contactVMs.Add(ContactViewModel.Create(client, contact));
			}

			foreach (var contact in changedContacts)
			{
				bool remove = false;
				var user = contact as IUser;
				var group = contact as IGroup;

				if (user != null)
				{
					switch (user.Relation)
					{
						case UserRelation.None:
							remove = true;
							break;
					
							// Don't include ourselves in any lists
						case UserRelation.Me:
							continue;

							// Add any users who just became our friends
						case UserRelation.Friend:
						case UserRelation.PendingFriend:
							if (contactVMs.SingleOrDefault(_ => _.Contact == contact) == null)
								contactVMs.Add(ContactViewModel.Create(client, contact));
							break;
					}
				}

				if (user != null && user.Relation == UserRelation.None)
					remove = true;
				else if (group != null && !group.Joined)
					remove = true;
				if (remove)
				{
					var vm = contactVMs.SingleOrDefault(_ => _.Contact == contact);
					if (vm != null)
						contactVMs.Remove(vm);
				}
			}

			contactsView.Refresh();

			NotifyPropertyChanged("Contacts");
			NotifyPropertyChanged("ContactsView");
		}

		class ContactComparer : System.Collections.IComparer
		{
			public int Compare(object __1, object __2)
			{
				if (!(__1 is ContactViewModel) || !(__2 is ContactViewModel))
					return 0;

				var _1 = (__1 as ContactViewModel).Contact;
				var _2 = (__2 as ContactViewModel).Contact;

				if (_1 is IGroup && _2 is IGroup)
					return String.Compare(_1.DisplayName, _2.DisplayName);
				else if (_1 is IGroup)
					return 1;
				else if (_2 is IGroup)
					return -1;
				else
					return String.Compare(_1.DisplayName, _2.DisplayName);
			}
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
