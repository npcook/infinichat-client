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
	public class SendChatEventArgs : EventArgs
	{
		public SendChatEventArgs(string message)
		{
			Message = message;
		}

		public readonly string Message;
	}

	/// <summary>
	/// Interaction logic for ChatPage.xaml
	/// </summary>
	public partial class ChatControl : UserControl
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly RoutedUICommand ChatInputEnter = new RoutedUICommand("ChatInputEnter", "ChatInputEnter", typeof(ChatControl));

		// TODO: Pruning of the dictionary isn't implemented, so it will keep growing
		private static readonly Dictionary<Color, SolidColorBrush> chatBrushCache = new Dictionary<Color, SolidColorBrush>();

		private Contact entity;
		private string lastSender;

		public event EventHandler<SendChatEventArgs> ChatSent;

		public ChatControl(Contact entity)
		{
			this.entity = entity;

			InitializeComponent();

			ChatHistory.Document.Blocks.Clear();
			ChangeEntity(entity);
		}

		public void ChangeEntity(Contact newEntity)
		{
			NameLabel.Content = newEntity.DisplayName;

			entity = newEntity;
			if (entity != null)
			{
				if (entity is Group)
				{
					StatusLabel.Visibility = System.Windows.Visibility.Collapsed;
					GroupMembersListBox.Visibility = System.Windows.Visibility.Visible;

					GroupMembersListBox.Items.Clear();
					var statusOrder = new List<UserStatus>(new UserStatus[] { UserStatus.Available, UserStatus.Away, UserStatus.Busy, UserStatus.Offline, UserStatus.Unknown });

					var group = entity as Group;
					var sortedMembers = new List<User>(group.Members);
					sortedMembers.Sort((_1, _2) =>
					{
						int comparison = statusOrder.IndexOf(_2.Status) - statusOrder.IndexOf(_1.Status);
						if (comparison == 0)
							comparison = _1.DisplayName.CompareTo(_2.DisplayName);
						return comparison;
					});

					foreach (var member in sortedMembers)
					{
						var item = new ListBoxItem()
						{
							Content = member.DisplayName,
							FontSize = 16,
							Foreground = App.GetUserStatusBrush(member.Status),
							Padding = new Thickness(4),
						};
						GroupMembersListBox.Items.Add(item);
					}
				}
				else if (entity is User)
				{
					GroupMembersListBox.Visibility = System.Windows.Visibility.Collapsed;
					StatusLabel.Visibility = System.Windows.Visibility.Visible;

					var user = entity as User;
					UserStatus status = user.Status;
					StatusLabel.Content = status.ToString();
					StatusLabel.Foreground = App.GetUserStatusBrush(status);
				}
			}
		}

		public void AddChatMessage(string sender, string message, Protocol.FontOptions font, DateTime timestamp)
		{
			// Add this color to the brush cache if it's not there already
			SolidColorBrush brush;
			if (!chatBrushCache.TryGetValue(font.Color, out brush))
			{
				brush = new SolidColorBrush(font.Color);
				brush.Freeze();
				chatBrushCache.Add(font.Color, brush);
			}

			Paragraph paragraph;
			if (sender != lastSender)
			{
				lastSender = sender;
				paragraph = new Paragraph()
				{
					Foreground = Brushes.DarkBlue,
					FontSize = FontSize + 1,
					Margin = new Thickness(0, 4.0, 0, 0),
				};
				paragraph.Inlines.Add(new Run(sender));
				ChatHistory.Document.Blocks.Add(paragraph);
			}

			paragraph = new Paragraph()
			{
				FontFamily = new FontFamily(font.Family + ",\"Segoe UI\""),
				Foreground = brush,
				LineHeight = 3 * FontSize / 2,
				Margin = new Thickness(16.0, 0, 0, 0),
			};
			paragraph.Inlines.Add(new Run(message));

			if (font.Style.HasFlag(Protocol.FontStyle.Bold))
				paragraph.FontWeight = FontWeights.Bold;
			if (font.Style.HasFlag(Protocol.FontStyle.Italic))
				paragraph.FontStyle = FontStyles.Italic;
			if (font.Style.HasFlag(Protocol.FontStyle.Underline))
			{
				paragraph.TextDecorations = new TextDecorationCollection();
				paragraph.TextDecorations.Add(TextDecorations.Underline);
			}

			ChatHistory.Document.Blocks.Add(paragraph);
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

			if (ChatInput.Text != "")
			{
				if (ChatSent != null)
					ChatSent(this, new SendChatEventArgs(ChatInput.Text));

				ChatInput.Text = "";
			}
			e.Handled = true;
		}

		private void ChatHistoryTextChanged(object sender, TextChangedEventArgs e)
		{
			// If the chat box is already scrolled to the bottom, keep it at the bottom.
			// Otherwise, the user has scrolled up and we don't want to disturb them.
			if (ChatHistory.VerticalOffset + ChatHistory.ExtentHeight == ChatHistory.ViewportHeight)
			{
				ChatHistory.ScrollToEnd();
			}
		}

		private void ChatHistoryTextInput(object sender, TextCompositionEventArgs e)
		{
			// Redirect input from the chat history to the chat input
			ChatInput.Focus();
			ChatInput.SelectedText = e.Text;
			ChatInput.Select(ChatInput.SelectionStart + e.Text.Length, 0);
		}

		private void ChatInputTextChanged(object sender, TextChangedEventArgs e)
		{
			// Handle emoticons eventually
		}
	}
}
