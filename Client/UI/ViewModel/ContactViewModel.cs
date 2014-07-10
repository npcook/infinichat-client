using Client.Protocol;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace Client.UI.ViewModel
{
	public class UserViewModel : ContactViewModel
	{
		public IUser User
		{ get { return Contact as IUser; } }

		string status;
		public string Status
		{
			get { return status; }
			private set
			{
				if (status != value)
				{
					status = value;
					NotifyPropertyChanged("Status");
				}
			}
		}

		public UserViewModel(ChatClient client, IUser entity)
			: base(client, entity)
		{ }

		protected override void OnContactChanged(object sender, EventArgs e)
		{
			base.OnContactChanged(sender, e);

			status = User.Status.ToString();
		}
	}

	public class GroupViewModel : ContactViewModel
	{
		public IGroup Group
		{ get { return Contact as IGroup; } }

		bool joined;
		public bool Joined
		{
			get { return joined; }
			private set
			{
				if (joined != value)
				{
					joined = value;
					NotifyPropertyChanged("Joined");
				}
			}
		}

		readonly ObservableCollection<UserViewModel> memberModels = new ObservableCollection<UserViewModel>();
		public ReadOnlyObservableCollection<UserViewModel> Members
		{
			get { return new ReadOnlyObservableCollection<UserViewModel>(memberModels); }
		}

		public GroupViewModel(ChatClient client, IGroup entity)
			: base(client, entity)
		{
			entity.UserAdded += OnUserAdded;
			entity.UserRemoved += OnUserRemoved;

			foreach (var member in entity.Members)
				memberModels.Add(new UserViewModel(client, member));
		}

		void OnUserAdded(object sender, UserEventArgs e)
		{
			memberModels.Add(new UserViewModel(client, e.User));
		}

		void OnUserRemoved(object sender, UserEventArgs e)
		{
			var member = memberModels.Single(vm => vm.User == e.User);
			memberModels.Remove(member);
			member.Dispose();
		}
		
		protected override void OnContactChanged(object sender, EventArgs e)
		{
			base.OnContactChanged(sender, e);

			Joined = Group.Joined;
		}

		bool disposed = false;
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				Group.UserAdded -= OnUserAdded;
				Group.UserRemoved -= OnUserRemoved;
			}
		}
	}

	public abstract class ContactViewModel : BaseViewModel
	{
		protected ChatClient client;

		string name;
		public string Name
		{
			get { return name; }
			private set
			{
				if (name != value)
				{
					name = value;
					NotifyPropertyChanged("Name");
				}
			}
		}

		string displayName;
		public string DisplayName
		{
			get { return displayName; }
			private set
			{
				if (displayName != value)
				{
					displayName = value;
					NotifyPropertyChanged("DisplayName");
				}
			}
		}

		Brush displayBrush;
		public Brush DisplayBrush
		{
			get { return displayBrush; }
			private set
			{
				if (displayBrush != value)
				{
					displayBrush = value;
					NotifyPropertyChanged("DisplayBrush");
				}
			}
		}

		readonly IContact contact;
		public IContact Contact
		{ get { return contact; } }

		public static ContactViewModel Create(ChatClient client, IContact contact)
		{
			if (contact is IUser)
				return new UserViewModel(client, contact as IUser);
			else if (contact is IGroup)
				return new GroupViewModel(client, contact as IGroup);
			else
				throw new NotSupportedException();
		}

		protected ContactViewModel(ChatClient client, IContact contact)
		{
			this.client = client;
			this.contact = contact;
			Contact.Changed += OnContactChanged;
			OnContactChanged(Contact, null);
		}

		protected virtual void OnContactChanged(object sender, EventArgs e)
		{
			Name = Contact.Name;
			DisplayName = Contact.DisplayName;
			DisplayBrush = Contact is IUser ? App.GetUserStatusBrush((Contact as IUser).Status) : Brushes.Black;
		}

		bool disposed = false;
		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);

			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				Contact.Changed -= OnContactChanged;
			}
		}
	}
}
