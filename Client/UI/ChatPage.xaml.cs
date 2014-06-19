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
	public partial class ChatPage : Page
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly RoutedUICommand ChatInputEnter = new RoutedUICommand("ChatInputEnter", "ChatInputEnter", typeof(ChatPage));

		// TODO: Pruning of the dictionary isn't implemented, so it will keep growing
		private static readonly Dictionary<Color, SolidColorBrush> chatBrushCache = new Dictionary<Color, SolidColorBrush>();
		private static readonly Dictionary<OnlineStatusCategory, SolidColorBrush> statusBrushCache = new Dictionary<OnlineStatusCategory, SolidColorBrush>();

		private ChatEntity entity;
		private ListBox groupMembersListBox;
		private Label statusLabel;

		public event EventHandler<SendChatEventArgs> OnChatSent;

		public ChatPage(ChatEntity entity)
		{
			InitializeComponent();

			if (statusBrushCache.Count == 0)
			{
				statusBrushCache.Add(OnlineStatusCategory.Online, new SolidColorBrush(Color.FromRgb(27, 195, 63)));
				statusBrushCache.Add(OnlineStatusCategory.Away, new SolidColorBrush(Color.FromRgb(228, 192, 62)));
				statusBrushCache.Add(OnlineStatusCategory.Busy, new SolidColorBrush(Color.FromRgb(195, 55, 4)));
				statusBrushCache.Add(OnlineStatusCategory.Invisible, new SolidColorBrush(Color.FromRgb(180, 180, 180)));
			}

			ChangeEntity(entity);
		}

		public void ChangeEntity(ChatEntity newEntity)
		{
			if (entity != null)
			{
				// TODO: Prevent the selection state for the listbox from being reset
				if (entity is Group && groupMembersListBox != null)
				{
					MainGrid.Children.Remove(groupMembersListBox);
					groupMembersListBox = null;
				}
				else if (entity is User && statusLabel != null)
				{
					MainGrid.Children.Remove(statusLabel);
					statusLabel = null;
				}
			}

			entity = newEntity;
			if (entity != null)
			{
				if (entity is Group)
				{
					groupMembersListBox = new ListBox()
					{
						Background = Brushes.Transparent,
						Margin = new Thickness(5),
						HorizontalAlignment = HorizontalAlignment.Stretch,
						VerticalAlignment = VerticalAlignment.Stretch,
					};
					ContactPanel.Children.Add(groupMembersListBox);

					var statusOrder = new List<OnlineStatusCategory>(new OnlineStatusCategory[] { OnlineStatusCategory.Online, OnlineStatusCategory.Away, OnlineStatusCategory.Busy, OnlineStatusCategory.Invisible, OnlineStatusCategory.Unknown });

					var group = entity as Group;
					var sortedMembers = new List<User>(group.Members.Values);
					sortedMembers.Sort((_1, _2) =>
						{
							int comparison = statusOrder.IndexOf(_2.OnlineStatus.Category) - statusOrder.IndexOf(_1.OnlineStatus.Category);
							if (comparison == 0)
								comparison = (_1.DisplayName ?? _1.Name).CompareTo(_2.DisplayName ?? _2.Name);
							return comparison;
						});

					foreach (var member in sortedMembers)
					{
						var item = new ListBoxItem()
						{
							Content = member.DisplayName ?? member.Name,
							FontSize = 16,
							Foreground = statusBrushCache[member.OnlineStatus.Category],
							Padding = new Thickness(4),
						};
						groupMembersListBox.Items.Add(item);
					}
				}
				else if (entity is User)
				{
					var user = entity as User;
					statusLabel = new Label()
					{
						Content = user.OnlineStatus.Display,
						Foreground = statusBrushCache[user.OnlineStatus.Category],
						FontWeight = FontWeights.Light,
						HorizontalAlignment = HorizontalAlignment.Center,
					};
					ContactPanel.Children.Add(statusLabel);
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

			var paragraph = new Paragraph()
			{
				Foreground = Brushes.DarkBlue,
				FontSize = FontSize + 1,
				Margin = new Thickness(0, 4.0, 0, 0),
			};
			paragraph.Inlines.Add(new Run(sender));
			ChatHistory.Document.Blocks.Add(paragraph);

			paragraph = new Paragraph()
			{
				FontFamily = new FontFamily(font.Family + ",\"Segoe UI\""),
				Foreground = brush,
				LineHeight = 3 * FontSize / 2,
				Margin = new Thickness(4.0, 0, 0, 0),
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
				if (OnChatSent != null)
					OnChatSent(this, new SendChatEventArgs(ChatInput.Text));

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
