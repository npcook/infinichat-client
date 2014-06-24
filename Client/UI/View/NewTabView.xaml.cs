using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Client.Protocol;
using Client.UI.ViewModel;

namespace Client.UI.View
{
	public class ListViewTemplateSelector : DataTemplateSelector
	{
		public DataTemplate UserTemplate
		{ get; set; }

		public DataTemplate GroupTemplate
		{ get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			if (item is UserViewModel)
				return UserTemplate;
			else if (item is GroupViewModel)
				return GroupTemplate;
			else
				return null;
		}
	}

	/// <summary>
	/// Interaction logic for NewTabView.xaml
	/// </summary>
	public partial class NewTabView : UserControl
	{
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

		public NewTabView()
		{
			InitializeComponent();

			DataContextChanged += (sender, e) =>
			{
				// TODO: Move to ViewModel

				(DataContext as NewTabViewModel).ContactsView.CustomSort = new ContactComparer();
			};
		}

		private void AddFriendButtonClick(object sender, RoutedEventArgs e)
		{
		}

		private void FriendListBoxDoubleClick(object sender, MouseButtonEventArgs e)
		{
			var vm = DataContext as NewTabViewModel;
			vm.StartChatCommand.Execute(null);
		}

		private void FriendListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
		}
	}
}
