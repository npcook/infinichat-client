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
	/// <summary>
	/// Interaction logic for NewTabView.xaml
	/// </summary>
	public partial class NewTabView : UserControl
	{
		public NewTabView()
		{
			InitializeComponent();
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
