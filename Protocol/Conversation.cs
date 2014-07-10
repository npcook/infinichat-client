using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Protocol
{
	public class Conversation
	{
		ChatClient client;
		List<Participant> participants = new List<Participant>();
		List<ChatMessage> chatLog = new List<ChatMessage>();
		ConversationEvents events;

		public event EventHandler<UserEventArgs> UserAdded;
		public event EventHandler<UserEventArgs> UserChanged;
		public event EventHandler<UserEventArgs> UserRemoved;
		public event EventHandler<ChatReceivedEventArgs> NewMessage;
		public event EventHandler<UserTypingEventArgs> UserTyping;
		public event EventHandler<EventArgs> Ended;

		public ICollection<Participant> Participants
		{ get { return participants; } }

		public bool IsGroup
		{ get { return participants.Count > 1; } }

		public IEnumerable<ChatMessage> RecentMessages
		{ get { return chatLog; } }

		public string Name
		{ get; private set; }

		public IContact Contact
		{ get; private set; }

		public bool HasNewMessages
		{ get; private set; }

		internal Conversation(ChatClient client, IContact who, ConversationEvents events)
		{
			this.client = client;
			this.events = events;
			Name = who.Name;
			Contact = who;

			events.UserAdded += OnUserAdded;
			events.UserChanged += OnUserChanged;
			events.UserRemoved += OnUserRemoved;
			events.UserTyping += OnUserTyping;
			events.ChatReceived += OnChatReceived;

			if (Contact is IUser)
			{
				participants.Add(new Participant(Contact as IUser));
			}
			else
			{
				foreach (var member in (Contact as IGroup).Members)
				{
					participants.Add(new Participant(member));
				}
			}
		}

		void OnUserAdded(object sender, UserEventArgs e)
		{
			participants.Add(new Participant(e.User));
			UserAdded.SafeInvoke(this, e);
		}

		void OnUserChanged(object sender, UserEventArgs e)
		{
			UserChanged.SafeInvoke(this, e);
		}

		void OnUserRemoved(object sender, UserEventArgs e)
		{
			participants.Remove(GetParticipant(e.User));
			UserRemoved.SafeInvoke(this, e);
			if (participants.Count == 0)
			{
				Ended.SafeInvoke(this, new EventArgs());

				events.UserAdded -= OnUserAdded;
				events.UserChanged -= OnUserChanged;
				events.UserRemoved -= OnUserRemoved;
				events.UserTyping -= OnUserTyping;
				events.ChatReceived -= OnChatReceived;
			}
		}

		void OnUserTyping(object sender, UserTypingEventArgs e)
		{
			GetParticipant(e.User).IsTyping = e.Starting;
			UserTyping.SafeInvoke(this, e);
		}

		void OnChatReceived(object sender, ChatReceivedEventArgs e)
		{
			AddMessage(e.Message);
		}

		public Participant GetParticipant(string name)
		{
			return participants.SingleOrDefault(participant => participant.User.Name == name);
		}

		public Participant GetParticipant(IUser user)
		{
			return participants.SingleOrDefault(participant => participant.User == user);
		}

		public void SendMessage(string message, FontOptions font)
		{
			SendMessage(message, font, DateTime.UtcNow);
		}

		public void SendMessage(string message, FontOptions font, DateTime timestamp)
		{
			if (IsGroup)
				client.ChatGroup(Name, font, message, timestamp, null);
			else
				client.ChatUser(Name, font, message, timestamp, null);

			AddMessage(new ChatMessage(client.Me, font, message, timestamp));
		}

		void AddMessage(ChatMessage message)
		{
			chatLog.Add(message);
			NewMessage.SafeInvoke(this, new ChatReceivedEventArgs(message));
		}
	}

	public class Participant
	{
		readonly IUser user;
		public IUser User
		{ get { return user; } }

		public bool IsTyping
		{ get; set; }

		public DateTime LastMessage
		{ get; set; }

		public Participant(IUser user)
		{
			this.user = user;
		}
	}

	public class ChatMessage
	{
		public readonly IUser Sender;
		public readonly FontOptions Font;
		public readonly string Text;
		public readonly DateTime Timestamp;

		public ChatMessage(IUser sender, FontOptions font, string text, DateTime timestamp)
		{
			Sender = sender;
			Font = font;
			Text = text;
			Timestamp = timestamp;
		}
	}
}
