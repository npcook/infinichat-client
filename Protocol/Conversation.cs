﻿using System;
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

		public event EventHandler<UserAddedEventArgs> UserAdded;
		public event EventHandler<UserRemovedEventArgs> UserRemoved;
		public event EventHandler<ChatReceivedEventArgs> ChatReceived;
		public event EventHandler<UserTypingEventArgs> UserTyping;
		public event EventHandler<EventArgs> Ended;

		public IEnumerable<Participant> Participants
		{ get { return participants; } }

		public bool IsGroup
		{ get { return participants.Count > 1; } }

		public IEnumerable<ChatMessage> RecentMessages
		{ get { return chatLog; } }

		public string Name
		{ get; private set; }

		public IContact Contact
		{ get; private set; }

		internal Conversation(ChatClient client, IContact who, ConversationEvents events)
		{
			this.client = client;
			this.events = events;
			Name = who.Name;
			Contact = who;

			events.UserAdded += events_UserAdded;
			events.UserRemoved += events_UserRemoved;
			events.UserTyping += events_UserTyping;
			events.ChatReceived += events_ChatReceived;
		}

		void events_UserAdded(object sender, UserAddedEventArgs e)
		{
			participants.Add(new Participant(e.User));
			UserAdded.SafeInvoke(this, e);
		}

		void events_UserRemoved(object sender, UserRemovedEventArgs e)
		{
			participants.Remove(GetParticipant(e.User.Name));
			UserRemoved.SafeInvoke(this, e);
			if (participants.Count == 0)
			{
				Ended.SafeInvoke(this, new EventArgs());

				events.UserAdded -= events_UserAdded;
				events.UserRemoved -= events_UserRemoved;
				events.UserTyping -= events_UserTyping;
				events.ChatReceived -= events_ChatReceived;
			}
		}

		void events_UserTyping(object sender, UserTypingEventArgs e)
		{
			GetParticipant(e.User.Name).IsTyping = e.Starting;
			UserTyping.SafeInvoke(this, e);
		}

		void events_ChatReceived(object sender, ChatReceivedEventArgs e)
		{
			chatLog.Add(e.Message);
			ChatReceived.SafeInvoke(this, e);
		}

		public Participant GetParticipant(string name)
		{
			return participants.SingleOrDefault(participant => participant.User.Name == name);
		}

		protected void OnChat(ChatReceivedEventArgs e)
		{
			ChatReceived.SafeInvoke(this, e);
		}

		public void SendMessage(string message, FontOptions font)
		{
			SendMessage(message, font, DateTime.UtcNow);
		}

		public void SendMessage(string message, FontOptions font, DateTime timestamp)
		{
			if (IsGroup)
				client.ChatGroup(Name, font, message, timestamp);
			else
				client.ChatUser(Name, font, message, timestamp);

			chatLog.Add(new ChatMessage(client.Me, font, message, timestamp));
		}
	}

	public class Participant
	{
		public IUser User = null;
		public bool IsTyping = false;
		public DateTime LastMessage = DateTime.MinValue;

		public Participant()
		{ }

		public Participant(IUser user)
		{
			User = user;
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