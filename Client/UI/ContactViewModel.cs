using System;
using System.Windows.Media;
using Client.Protocol;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Client.UI
{
	public class UserViewModel : ContactViewModel
	{
		public new User Contact;

		public UserViewModel(ChatClient client, Contact entity)
			: base(client, entity)
		{ }

		public override void UpdateContact(Contact newEntity)
		{
			if (Contact != null)
			{
				string oldStatus = Status;

				base.UpdateContact(newEntity);
				Contact = newEntity as User;

				if (oldStatus != Status)
					NotifyPropertyChanged("Status");
			}
			else
			{
				base.UpdateContact(newEntity);
				Contact = newEntity as User;
			}
		}

		public string Status
		{
			get
			{
				return Contact.Status.ToString();
			}
		}
	}

	public class GroupViewModel : ContactViewModel
	{
		protected readonly ObservableCollection<UserViewModel> memberModels = new ObservableCollection<UserViewModel>();

		public new Group Contact;

		public GroupViewModel(ChatClient client, Contact entity)
			: base(client, entity)
		{ }

		public override void UpdateContact(Contact newEntity)
		{
			if (Contact != null)
			{
				var oldMembers = (Contact as Group).Members;

				base.UpdateContact(newEntity);
				Contact = newEntity as Group;

				var members = Contact.Members;

				for (int i = 0; i < members.Count; ++i)
				{
					var member = members[i];
					if (!oldMembers.Contains(member))
						memberModels.Insert(i, new UserViewModel(client, member));
				}
				for (int i = 0; i < oldMembers.Count; ++i)
				{
					var member = members[i];
					if (!members.Contains(member))
						memberModels.RemoveAt(i);
				}
			}
			else
			{
				base.UpdateContact(newEntity);
				Contact = newEntity as Group;
				foreach (var member in Contact.Members)
					memberModels.Add(new UserViewModel(client, member));
			}
		}

		public ReadOnlyObservableCollection<UserViewModel> Members
		{
			get { return new ReadOnlyObservableCollection<UserViewModel>(memberModels); }
		}

		public bool Joined
		{ get { return Contact.Joined; } }
	}

	public class ContactViewModel : BaseViewModel
	{
		protected ChatClient client;

		public Contact Contact
		{ get; protected set; }

		public static ContactViewModel Create(ChatClient client, Contact contact)
		{
			if (contact is User)
				return new UserViewModel(client, contact);
			else if (contact is Group)
				return new GroupViewModel(client, contact);
			else
				throw new NotSupportedException();
		}

		protected ContactViewModel(ChatClient client, Contact entity)
		{
			this.client = client;

			UpdateContact(entity);
		}

		public virtual void UpdateContact(Contact newEntity)
		{
			if (Contact != null)
			{
				string oldName = Name;
				string oldDisplayName = DisplayName;
				Brush oldDisplayBrush = DisplayBrush;

				Contact = newEntity;

				if (oldName != Name)
					NotifyPropertyChanged("Name");
				if (oldDisplayName != DisplayName)
					NotifyPropertyChanged("DisplayName");
				if (oldDisplayBrush != DisplayBrush)
					NotifyPropertyChanged("DisplayBrush");
			}
			else
				Contact = newEntity;
		}

		public string Name
		{
			get
			{
				return Contact.Name;
			}
		}

		public string DisplayName
		{
			get
			{
				return Contact.DisplayName;
			}
		}

		public Brush DisplayBrush
		{
			get
			{
				return Contact is User ? App.GetUserStatusBrush((Contact as User).Status) : Brushes.Gray;
			}
		}
	}
}
