using Client.Protocol;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace Client.UI.ViewModel
{
	class ConversationViewModel : BaseViewModel
	{
		ChatClient client;
		ObservableCollection<UserViewModel> participants = new ObservableCollection<UserViewModel>();
		ObservableCollection<UserViewModel> typingParticipants = new ObservableCollection<UserViewModel>();
		IUser lastSender;
		string currentMessage = "";
		RelayCommand sendChatCommand;
		FontOptions font;

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
			ChatHistory.PagePadding = new Thickness(0);
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
				Conversation.Contact.Changed -= Contact_Changed;
				Conversation.UserAdded -= Conversation_UserAdded;
				Conversation.UserRemoved -= Conversation_UserRemoved;
				Conversation.ChatReceived -= Conversation_ChatReceived;
				Conversation.UserTyping -= Conversation_UserTyping;
				Conversation.Ended -= Conversation_Ended;
			}

			Conversation = convo;

			Conversation.Contact.Changed += Contact_Changed;
			Conversation.UserAdded += Conversation_UserAdded;
			Conversation.UserRemoved += Conversation_UserRemoved;
			Conversation.ChatReceived += Conversation_ChatReceived;
			Conversation.UserTyping += Conversation_UserTyping;
			Conversation.Ended += Conversation_Ended;

			participants.Clear();
			foreach (var participant in Conversation.Participants)
				participants.Add(new UserViewModel(client, participant.User));
			typingParticipants.Clear();
		}

		void Contact_Changed(object sender, EventArgs e)
		{
			NotifyPropertyChanged("Title");
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
					Foreground = Brushes.DarkBlue,
					FontSize = ChatHistory.FontSize + 1,
					Margin = new Thickness(0, 4.0, 0, 0),
				};
				paragraph.Inlines.Add(new Run(sender.DisplayName));
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

		void Conversation_UserRemoved(object sender, UserRemovedEventArgs e)
		{
			participants.Remove(participants.Single(vm => vm.Contact == e.User));
		}

		void Conversation_UserAdded(object sender, UserAddedEventArgs e)
		{
			participants.Add(new UserViewModel(client, e.User));
		}
	}
}
