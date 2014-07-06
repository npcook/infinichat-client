using Client.Protocol;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;

namespace Client.UI.ViewModel
{
	public class UserViewModel : ContactViewModel
	{
		string status;

		public IUser User
		{ get { return Contact as IUser; } }

		public UserViewModel(ChatClient client, IUser entity)
			: base(client, entity)
		{ }

		protected override void OnContactChanged(object sender, EventArgs e)
		{
			base.OnContactChanged(sender, e);

			var oldStatus = status;

			status = User.Status.ToString();

			if (oldStatus != status)
				NotifyPropertyChanged("Status");
		}

		public string Status
		{ get { return status; } }
	}

	public class GroupViewModel : ContactViewModel
	{
		readonly ObservableCollection<UserViewModel> memberModels = new ObservableCollection<UserViewModel>();
		bool joined;

		public IGroup Group
		{ get { return Contact as IGroup; } }

		public GroupViewModel(ChatClient client, IGroup entity)
			: base(client, entity)
		{
			entity.UserAdded += OnUserAdded;
			entity.UserRemoved += OnUserRemoved;
		}

		void OnUserAdded(object sender, UserEventArgs e)
		{
			memberModels.Add(new UserViewModel(client, e.User));
		}

		void OnUserRemoved(object sender, UserEventArgs e)
		{
			memberModels.Remove(memberModels.Single(vm => vm.Name == e.User.Name));
		}
		
		protected override void OnContactChanged(object sender, EventArgs e)
		{
			base.OnContactChanged(sender, e);

			var oldJoined = joined;
			joined = Group.Joined;

			if (oldJoined != joined)
				NotifyPropertyChanged("Joined");
		}

		public ReadOnlyObservableCollection<UserViewModel> Members
		{
			get { return new ReadOnlyObservableCollection<UserViewModel>(memberModels); }
		}
	}

	public class ContactViewModel : BaseViewModel
	{
		protected ChatClient client;
		string name;
		string displayName;
		Brush displayBrush;

		public string Name
		{ get { return name; } }

		public string DisplayName
		{ get { return displayName; } }

		public Brush DisplayBrush
		{ get { return displayBrush; } }

		public IContact Contact
		{ get; protected set; }

		public static ContactViewModel Create(ChatClient client, IContact contact)
		{
			if (contact is IUser)
				return new UserViewModel(client, contact as IUser);
			else if (contact is IGroup)
				return new GroupViewModel(client, contact as IGroup);
			else
				throw new NotSupportedException();
		}

		protected ContactViewModel(ChatClient client, IContact entity)
		{
			this.client = client;
			Contact = entity;
			Contact.Changed += OnContactChanged;
			OnContactChanged(Contact, null);
		}

		protected virtual void OnContactChanged(object sender, EventArgs e)
		{
			var oldName = name;
			var oldDisplayName = displayName;
			var oldDisplayBrush = displayBrush;

			name = Contact.Name;
			displayName = Contact.DisplayName;
			displayBrush = Contact is IUser ? App.GetUserStatusBrush((Contact as IUser).Status) : Brushes.Gray;

			if (oldName != name)
				NotifyPropertyChanged("Name");
			if (oldDisplayName != displayName)
				NotifyPropertyChanged("DisplayName");
			if (oldDisplayBrush != displayBrush)
				NotifyPropertyChanged("DisplayBrush");
		}
	}
}
