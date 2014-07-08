using Client.Protocol;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Timers;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.UI.ViewModel
{
	class ConversationViewModel : CloseableViewModel
	{
		ChatClient client;
		ObservableCollection<UserViewModel> participants = new ObservableCollection<UserViewModel>();
		ObservableCollection<UserViewModel> typingParticipants = new ObservableCollection<UserViewModel>();
		IUser lastSender;
		string currentMessage = "";
		RelayCommand sendChatCommand;
		FontOptions font;
		bool isHighlighted;

		public ContactViewModel Contact
		{ get; private set; }

		public Conversation Conversation
		{ get; private set; }

		public ReadOnlyObservableCollection<UserViewModel> Participants
		{ get { return new ReadOnlyObservableCollection<UserViewModel>(participants); } }

		public ReadOnlyObservableCollection<UserViewModel> TypingParticipants
		{ get { return new ReadOnlyObservableCollection<UserViewModel>(typingParticipants); } }

		public FlowDocument ChatHistory
		{ get; private set; }

		public string CurrentMessage
		{
			get { return currentMessage; }
			set
			{
				currentMessage = value;
				NotifyPropertyChanged("CurrentMessage");
			}
		}

		public ICommand SendChatCommand
		{ get { return sendChatCommand; } }

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

		public string Title
		{ get { return Conversation.Contact.DisplayName; } }

		public Brush TitleBrush
		{ get { return Brushes.Black; } }

		public string Subtitle
		{
			get
			{
				if (Conversation.IsGroup)
					return "";
				else
					return (Conversation.Contact as IUser).Status.ToString();
			}
		}

		public Brush SubtitleBrush
		{
			get
			{
				if (Conversation.IsGroup)
					return Brushes.Transparent;
				else
					return App.GetUserStatusBrush((Conversation.Contact as IUser).Status);
			}
		}

		public bool IsHighlighted
		{
			get { return isHighlighted; }
			set
			{
				isHighlighted = value;
				NotifyPropertyChanged("IsHighlighted");
			}
		}

		void OnHighlightElapsed(object sender, ElapsedEventArgs e)
		{
			IsHighlighted = !IsHighlighted;
		}

		public ConversationViewModel(ChatClient client, Conversation convo)
		{
			this.client = client;

			sendChatCommand = new RelayCommand(_ =>
				{
					Conversation.SendMessage(CurrentMessage, App.Current.ClientFont);
					Conversation_ChatReceived(this, new ChatReceivedEventArgs(new ChatMessage(client.Me, font, CurrentMessage, DateTime.UtcNow)));
					CurrentMessage = "";
				}, _ => CurrentMessage.Length > 0);

			ChatHistory = new FlowDocument();
			ChatHistory.Background = Brushes.White;
			ChatHistory.PagePadding = new Thickness(4);
			ChatHistory.FontFamily = new FontFamily("Segoe UI");
			ChatHistory.FontSize = new FontSizeConverter().ConvertFromString("10pt") as double? ?? 0;

			App.Current.FontChanged += OnFontChanged;
			OnFontChanged(this, null);

			SetConversation(convo);
		}

		void OnFontChanged(object sender, EventArgs e)
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

		public void SetConversation(Conversation convo)
		{
			if (Conversation != null)
			{
				typingParticipants.Clear();
				foreach (var participant in participants)
					participant.Dispose();
				participants.Clear();

				Conversation.Contact.Changed -= Contact_Changed;
				Conversation.UserAdded -= Conversation_UserAdded;
				Conversation.UserChanged -= Conversation_UserChanged;
				Conversation.UserRemoved -= Conversation_UserRemoved;
				Conversation.ChatReceived -= Conversation_ChatReceived;
				Conversation.UserTyping -= Conversation_UserTyping;
				Conversation.Ended -= Conversation_Ended;

				Contact = null;
			}

			Conversation = convo;

			if (Conversation != null)
			{
				Conversation.Contact.Changed += Contact_Changed;
				Conversation.UserAdded += Conversation_UserAdded;
				Conversation.UserChanged += Conversation_UserChanged;
				Conversation.UserRemoved += Conversation_UserRemoved;
				Conversation.ChatReceived += Conversation_ChatReceived;
				Conversation.UserTyping += Conversation_UserTyping;
				Conversation.Ended += Conversation_Ended;

				foreach (var participant in Conversation.Participants)
					participants.Add(new UserViewModel(client, participant.User));

				Contact = ContactViewModel.Create(client, Conversation.Contact);
			}
			NotifyPropertyChanged("Contact");
		}

		void Conversation_UserChanged(object sender, UserEventArgs e)
		{
//			throw new NotImplementedException();
		}

		void Contact_Changed(object sender, EventArgs e)
		{
			NotifyPropertyChanged("Title");
			NotifyPropertyChanged("TitleBrush");
			NotifyPropertyChanged("Subtitle");
			NotifyPropertyChanged("SubtitleBrush");
		}

		void Conversation_Ended(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		void Conversation_UserTyping(object sender, UserTypingEventArgs e)
		{
			var participant = participants.Single(vm => vm.Contact == e.User);
			if (e.Starting)
				typingParticipants.Add(participant);
			else
				typingParticipants.Remove(participant);
		}

		void Conversation_ChatReceived(object _sender, ChatReceivedEventArgs e)
		{
			App.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				var message = e.Message.Text;
				var font = e.Message.Font;
				var sender = e.Message.Sender;

				// Add this color to the brush cache if it's not there already
				var brush = App.GetBrush(font.Color);

				Paragraph paragraph;
				if (sender != lastSender)
				{
					lastSender = sender;
					paragraph = new Paragraph()
					{
						Foreground = Brushes.Black,
						FontSize = ChatHistory.FontSize + 2,
						FontWeight = FontWeights.Light,
						Margin = new Thickness(0, 4.0, 0, 2.0),
						Padding = new Thickness(0, 0, 0, 2.0),
						BorderBrush = Brushes.LightGray,
						BorderThickness = new Thickness(0, 0, 0, 1),
					};
					paragraph.Inlines.Add(new Run(sender.DisplayName));
					ChatHistory.Blocks.Add(paragraph);
				}

				paragraph = new Paragraph()
				{
					FontFamily = new FontFamily(font.Family),
					Foreground = brush,
					LineHeight = 3 * ChatHistory.FontSize / 2,
					Margin = new Thickness(16.0, 0, 0, 0),
					TextIndent = 0,
				};

/*				var path = new System.Windows.Shapes.Path()
				{
					Stroke = Brushes.Black,
					StrokeThickness = 2,
					Data = App.Current.FindResource("CrossGeometry") as Geometry,
					Width = 12,
					Height = paragraph.FontSize,
					Margin = new Thickness(0, 0, 4, 0),
				};

				paragraph.Inlines.Add(new InlineUIContainer(path));*/

				var indexList = App.Current.SearchForEmoticons(message);

				int startIndex = 0;
				for (int i = 0; i < indexList.Count; ++i)
				{
					paragraph.Inlines.Add(new Run(message.Substring(startIndex, indexList[i].Key - startIndex)));

					var picture = new EmoteImage();
					picture.Emote = indexList[i].Value;

					paragraph.Inlines.Add(new InlineUIContainer(picture));

					startIndex = indexList[i].Key + indexList[i].Value.Shortcut.Length;
				}
				paragraph.Inlines.Add(new Run(message.Substring(startIndex)));

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
			}));
		}

		void Conversation_UserRemoved(object sender, UserEventArgs e)
		{
			var participant = participants.Single(vm => vm.Contact == e.User);
			participants.Remove(participants.Single(vm => vm.Contact == e.User));
			typingParticipants.Remove(participant);
			participant.Dispose();
		}

		void Conversation_UserAdded(object sender, UserEventArgs e)
		{
			participants.Add(new UserViewModel(client, e.User));
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
				App.Current.FontChanged -= OnFontChanged;

				SetConversation(null);
			}
		}
	}
}
