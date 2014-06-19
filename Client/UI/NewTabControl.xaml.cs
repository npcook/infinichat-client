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

namespace Client.UI
{
	/// <summary>
	/// Interaction logic for NewTabControl.xaml
	/// </summary>
	public partial class NewTabControl : UserControl
	{
		public event EventHandler<StartChatEventArgs> StartChat;

		public NewTabControl()
		{
			InitializeComponent();
		}

		public void UpdateContacts(IEnumerable<Contact> addedContacts, IEnumerable<Contact> changedContacts)
		{
/*			FriendListBox.Items.Clear();
			foreach (var entity in contacts)
			{
				var entityItemContent = new StackPanel()
				{
					Orientation = Orientation.Horizontal,
				};
				entityItemContent.Children.Add(new Rectangle()
				{
					Width = 16,
					Height = 16,
					Fill = entity is User ? App.GetUserStatusBrush((entity as User).Status.Category) : Brushes.Gray,
				});
				entityItemContent.Children.Add(new TextBlock()
				{
					Text = entity.DisplayName,
				});

				var entityItem = new TreeViewItem()
				{
					Header = entityItemContent,
				};
				entityItem.PreviewMouseDown += (sender, e) =>
					{
						if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed && e.ClickCount == 2)
						{
							var handler = StartChat;
							if (handler != null)
								handler(this, new StartChatEventArgs(entity));
						}
					};

				FriendListBox.Items.Add(entityItem);
			}*/
		}

		private void AddFriendButtonClick(object sender, RoutedEventArgs e)
		{
			var shit = AddFriendTextBox.Text;
			int separatorIndex = shit.IndexOf(':');
			var username = shit.Substring(0, separatorIndex);
			var message = shit.Substring(separatorIndex + 1);

//			(App.Current.MainWindow as MainWindow).client.ChatUser(username, new Protocol.FontOptions() { Family = "Comic Sans MS", Color = Color.FromRgb(255, 21, 194), Style = Protocol.FontStyle.Bold }, message);
		}
	}
}
