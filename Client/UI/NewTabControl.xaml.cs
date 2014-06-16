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

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for NewTabControl.xaml
	/// </summary>
	public partial class NewTabControl : UserControl
	{
		public NewTabControl()
		{
			InitializeComponent();
		}

		private void FriendTreeViewSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
		{

		}

		private void AddFriendButtonClick(object sender, RoutedEventArgs e)
		{
			var shit = AddFriendTextBox.Text;
			int separatorIndex = shit.IndexOf(':');
			var username = shit.Substring(0, separatorIndex);
			var message = shit.Substring(separatorIndex + 1);

			(App.Current.MainWindow as MainWindow).client.ChatUser(username, new Protocol.FontOptions() { Family = "Comic Sans MS", Color = Color.FromRgb(255, 21, 194), Style = Protocol.FontStyle.Bold }, message);
		}
	}
}
