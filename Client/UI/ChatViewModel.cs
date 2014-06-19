using Client.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.UI
{
	class ChatViewModel : BaseViewModel
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public static readonly RoutedUICommand ChatInputEnter = new RoutedUICommand("ChatInputEnter", "ChatInputEnter", typeof(ChatControl));

		// TODO: Pruning of the dictionary isn't implemented, so it will keep growing
		private static readonly Dictionary<Color, SolidColorBrush> chatBrushCache = new Dictionary<Color, SolidColorBrush>();
		private FontOptions font;

		private ChatClient client;
		private string lastSender;

		public ContactViewModel Contact
		{ get; protected set; }

		public bool IsGroup
		{ get { return Contact is GroupViewModel; } }

		public FlowDocument ChatHistory
		{ get; private set; }

		public string FontFamily
		{ get { return font.Family; } }

		public System.Windows.FontStyle FontStyle
		{ get { return font.Style.HasFlag(Protocol.FontStyle.Italic) ? FontStyles.Italic : FontStyles.Normal; } }

		public FontWeight FontWeight
		{ get { return font.Style.HasFlag(Protocol.FontStyle.Bold) ? FontWeights.Bold : FontWeights.Normal; } }

		public Color FontColor
		{ get { return font.Color; } }

		public TextDecorationCollection TextDecorations
		{ get; private set; }

		public event EventHandler<SendChatEventArgs> ChatSent;
		public event EventHandler<ChatEventArgs> ChatReceived;

		public ICommand SendChatCommand
		{
			get
			{
				return new RelayCommand(
					text =>
					{
						if (Contact.Contact is User)
							client.ChatUser(Contact.Name, font, text as string);
						else
							client.ChatGroup(Contact.Name, font, text as string);
						AddChatMessage(client.Me.DisplayName, text as string, font, DateTime.UtcNow);
					},
					text => (text as string).Length > 0
					);
			}
		}

		public ChatViewModel(ChatClient client, Contact contact)
		{
			this.client = client;
			ChatHistory = new FlowDocument();
			ChatHistory.FontFamily = new FontFamily("Segoe UI");
			ChatHistory.FontSize = new FontSizeConverter().ConvertFromString("10pt") as double? ?? 0;

			App.Current.FontChanged += OnFontChanged;
			OnFontChanged(this, null);

			ChangeEntity(contact);
		}

		private void OnFontChanged(object sender, EventArgs e)
		{
			font = App.Current.ClientFont;

			TextDecorations = new TextDecorationCollection();
			if (font.Style.HasFlag(Protocol.FontStyle.Underline))
				TextDecorations.Add(System.Windows.TextDecorations.Underline);

			NotifyPropertyChanged("FontFamily");
			NotifyPropertyChanged("FontWeight");
			NotifyPropertyChanged("FontStyle");
			NotifyPropertyChanged("FontColor");
			NotifyPropertyChanged("TextDecorations");
		}

		public void ChangeEntity(Contact newContact)
		{
			Contact = ContactViewModel.Create(client, newContact);

			NotifyPropertyChanged("Contact");
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
					FontSize = ChatHistory.FontSize + 1,
					Margin = new Thickness(0, 4.0, 0, 0),
				};
				paragraph.Inlines.Add(new Run(sender));
				ChatHistory.Blocks.Add(paragraph);
			}

			paragraph = new Paragraph()
			{
				FontFamily = new FontFamily(font.Family + ",Segoe UI"),
				Foreground = brush,
				LineHeight = 3 * ChatHistory.FontSize / 2,
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
				paragraph.TextDecorations.Add(System.Windows.TextDecorations.Underline);
			}

			ChatHistory.Blocks.Add(paragraph);
		}
	}
}
