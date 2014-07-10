using Client.Protocol;
using System;
using System.Collections.Generic;
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
		RelayCommand sendChatCommand;
		FontOptions font;

		ContactViewModel contact;
		public ContactViewModel Contact
		{
			get { return contact; }
			private set
			{
				contact = value;
				NotifyPropertyChanged("Contact");
			}
		}

		Conversation conversation;
		public Conversation Conversation
		{
			get { return conversation; }
			private set
			{
				conversation = value;
				NotifyPropertyChanged("Conversation");
			}
		}

		public ReadOnlyObservableCollection<UserViewModel> Participants
		{ get { return new ReadOnlyObservableCollection<UserViewModel>(participants); } }

		public ReadOnlyObservableCollection<UserViewModel> TypingParticipants
		{ get { return new ReadOnlyObservableCollection<UserViewModel>(typingParticipants); } }

		readonly ObservableCollection<Block> chatHistory = new ObservableCollection<Block>();
		public ObservableCollection<Block> ChatHistory
		{ get { return chatHistory; } }

		string currentMessage = "";
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

		bool isHighlighted;
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
					CurrentMessage = "";
				}, _ => CurrentMessage.Length > 0);

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

				Conversation.Contact.Changed -= OnContactChanged;
				Conversation.UserAdded -= OnUserAdded;
				Conversation.UserChanged -= OnUserChanged;
				Conversation.UserRemoved -= OnUserRemoved;
				Conversation.NewMessage -= OnChatReceived;
				Conversation.UserTyping -= OnUserTyping;
				Conversation.Ended -= OnEnded;
			}

			Conversation = convo;

			var oldContact = Contact;
			if (convo != null)
			{
				convo.Contact.Changed += OnContactChanged;
				convo.UserAdded += OnUserAdded;
				convo.UserChanged += OnUserChanged;
				convo.UserRemoved += OnUserRemoved;
				convo.NewMessage += OnChatReceived;
				convo.UserTyping += OnUserTyping;
				convo.Ended += OnEnded;

				foreach (var participant in convo.Participants)
					participants.Add(new UserViewModel(client, participant.User));

				Contact = ContactViewModel.Create(client, convo.Contact);
			}
			else
				Contact = null;

			if (oldContact != null)
				oldContact.Dispose();
		}

		void OnUserChanged(object sender, UserEventArgs e)
		{
//			throw new NotImplementedException();
		}

		void OnContactChanged(object sender, EventArgs e)
		{
			NotifyPropertyChanged("Title");
			NotifyPropertyChanged("TitleBrush");
			NotifyPropertyChanged("Subtitle");
			NotifyPropertyChanged("SubtitleBrush");
		}

		void OnEnded(object sender, EventArgs e)
		{
			throw new NotImplementedException();
		}

		void OnUserTyping(object sender, UserTypingEventArgs e)
		{
			var participant = participants.Single(vm => vm.Contact == e.User);
			if (e.Starting)
				typingParticipants.Add(participant);
			else
				typingParticipants.Remove(participant);
		}

		enum StopType
		{
			Url,
			Formatting,
			Emoticon,
		}

		struct ChatStop
		{
			public int Index;
			public int Length;
			public StopType Type;
			public object Data;
		}

		void OnChatReceived(object _sender, ChatReceivedEventArgs e)
		{
			App.Current.Dispatcher.BeginInvoke(new Action(() =>
			{
				const int FontSize = 14;

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
						FontSize = FontSize + 2,
						Foreground = Brushes.Black,
						FontWeight = FontWeights.Light,
						Margin = new Thickness(0, 4.0, 0, 2.0),
						Padding = new Thickness(0, 0, 0, 2.0),
						BorderBrush = Brushes.LightGray,
						BorderThickness = new Thickness(0, 0, 0, 1),
					};
					paragraph.Inlines.Add(new Run(sender.DisplayName));
					ChatHistory.Add(paragraph);
				}

				paragraph = new Paragraph()
				{
					FontFamily = new FontFamily(font.Family),
					FontSize = FontSize,
					LineHeight = 3 * FontSize / 2,
					Foreground = brush,
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

				var stops = new List<ChatStop>();

				foreach (var emoteStop in App.EmoticonManager.SearchForEmoticons(message))
				{
					stops.Add(new ChatStop()
						{
							Index = emoteStop.Key,
							Length = emoteStop.Value.Shortcut.Length,
							Type = StopType.Emoticon,
							Data = emoteStop.Value,
						});
				}

				string[] urls = new string[] {"http://", "https://"};
				foreach (var url in urls)
				{
					int index = message.IndexOf(url);
					while (index != -1)
					{
						int endIndex = message.IndexOf(' ', index);
						if (endIndex == -1)
							endIndex = message.Length - 1;
						stops.Add(new ChatStop()
							{
								Index = index,
								Length = endIndex - index + 1,
								Type = StopType.Url,
							});

						index = message.IndexOf(url, endIndex);
					}
				}

				var orderedStops = stops.OrderBy(_ => _.Index);

				int startIndex = 0;
				foreach (var stop in orderedStops)
				{
					if (stop.Index < startIndex)
						continue;
					if (stop.Index != startIndex)
						paragraph.Inlines.Add(new Run(message.Substring(startIndex, stop.Index - startIndex)));

					switch (stop.Type)
					{
						case StopType.Emoticon:
							var picture = new EmoteImage()
							{
								SnapsToDevicePixels = true,
								Emote = stop.Data as Emoticon,
							};

							paragraph.Inlines.Add(new InlineUIContainer(picture));
							break;

						case StopType.Url:
							var text = message.Substring(stop.Index, stop.Length);
							var link = new Hyperlink(new Run(text));
							link.Tag = text;
							link.Click += OnChatLinkClicked;
							paragraph.Inlines.Add(link);
							break;
					}

					startIndex = stop.Index + stop.Length;
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

				ChatHistory.Add(paragraph);
			}));
		}

		private void OnChatLinkClicked(object sender, RoutedEventArgs e)
		{
			System.Diagnostics.Process.Start((sender as Hyperlink).Tag as string);
		}

		void OnUserRemoved(object sender, UserEventArgs e)
		{
			var participant = participants.Single(vm => vm.Contact == e.User);
			participants.Remove(participants.Single(vm => vm.Contact == e.User));
			typingParticipants.Remove(participant);
			participant.Dispose();
		}

		void OnUserAdded(object sender, UserEventArgs e)
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
