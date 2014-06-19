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
	/// Interaction logic for ChatView.xaml
	/// </summary>
	public partial class ChatView : UserControl
	{
		public static readonly RoutedUICommand ChatInputEnter = new RoutedUICommand("ChatInputEnter", "ChatInputEnter", typeof(ChatControl));

		public ChatView()
		{
			InitializeComponent();

			DataContextChanged += (sender, e) =>
				{
					var vm = DataContext as ChatViewModel;
					if (vm.IsGroup)
						GroupMembersListBox.Visibility = System.Windows.Visibility.Visible;
					else
						StatusLabel.Visibility = System.Windows.Visibility.Visible;
				};
		}

		private void ChatHistoryTextChanged(object sender, TextChangedEventArgs e)
		{
			// If the chat box is already scrolled to the bottom, keep it at the bottom.
			// Otherwise, the user has scrolled up and we don't want to disturb them.
/*			if (ChatHistory.VerticalOffset + ChatHistory.ExtentHeight == ChatHistory.ViewportHeight)
			{
				ChatHistory.ScrollToEnd();
			}*/
		}

		private void ChatInputEnterCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = true;
		}

		private void ChatInputEnterExecute(object sender, ExecutedRoutedEventArgs e)
		{
			// If either control key is held down, add a linebreak instead of sending the message
			if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
				return;

			var vm = DataContext as ChatViewModel;
			if (vm.SendChatCommand.CanExecute(ChatInput.Text))
			{
				vm.SendChatCommand.Execute(ChatInput.Text);
				ChatInput.Text = "";

			}
			e.Handled = true;
		}
	}
}
