using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Protocol
{
	public class NewConversationEventArgs : EventArgs
	{
		public readonly Conversation Conversation;

		public NewConversationEventArgs(Conversation convo)
		{
			Conversation = convo;
		}
	}

	public class ConversationManager
	{
		readonly ChatClient client;
		readonly List<Conversation> conversations = new List<Conversation>();
		readonly Dictionary<string, ConversationEvents> conversationEventsMap = new Dictionary<string, ConversationEvents>();

		public event EventHandler<NewConversationEventArgs> NewConversation;

		public IEnumerable<Conversation> Conversations
		{ get { return conversations; } }

		public ConversationManager(ChatClient client)
		{
			this.client = client;
			client.UserChat += client_UserChat;
			client.GroupChat += client_GroupChat;
			client.UserDetailsChange += client_UserDetailsChange;
			client.GroupDetailsChange += client_GroupDetailsChange;
		}

		Conversation FindConversation(string name)
		{
			return conversations.SingleOrDefault(convo => convo.Name == name);
		}

		void client_UserChat(object sender, UserChatEventArgs e)
		{
			var _e = new ChatReceivedEventArgs(new ChatMessage(e.User, e.Font, e.Body, e.Timestamp));

			if (!conversationEventsMap.ContainsKey(e.User.Name))
			{
				CreateConversation(e.User);
			}

			conversationEventsMap[e.User.Name].RaiseChatReceived(_e);
		}

		void client_GroupChat(object sender, GroupChatEventArgs e)
		{
			var _e = new ChatReceivedEventArgs(new ChatMessage(e.User, e.Font, e.Body, e.Timestamp));

			if (!conversationEventsMap.ContainsKey(e.Group.Name))
			{
				CreateConversation(e.Group);
			}

			conversationEventsMap[e.Group.Name].RaiseChatReceived(_e);
		}

		void client_UserDetailsChange(object sender, UserDetailsEventArgs e)
		{
			foreach (var user in e.ChangedUsers)
			{
				foreach (var convo in conversations)
				{
					var participant = convo.Participants.SingleOrDefault(_ => _.User == user);
					if (participant != null)
						conversationEventsMap[convo.Contact.Name].RaiseUserChanged(new UserEventArgs(participant.User));
				}
			}
		}

		void client_GroupDetailsChange(object sender, GroupDetailsEventArgs e)
		{
			foreach (var group in e.ChangedGroups)
			{
				var convo = FindConversation(group.Name);
				if (convo == null)
					continue;

				var oldMembers = (from participant in convo.Participants select participant.User).ToList();
				foreach (var member in group.Members)
				{
					if (!oldMembers.Contains(member))
						conversationEventsMap[convo.Name].RaiseUserAdded(new UserEventArgs(member));
				}
				foreach (var member in oldMembers)
				{
					if (!group.Members.Contains(member))
						conversationEventsMap[convo.Name].RaiseUserRemoved(new UserEventArgs(member));
				}
			}
		}

		public Conversation CreateConversation(IContact who)
		{
			Conversation convo;
			var events = new ConversationEvents();
			if (who is User)
				convo = new Conversation(client, who as User, events);
			else if (who is Group)
				convo = new Conversation(client, who as Group, events);
			else
				throw new ArgumentException("who is not a User or a Group");

			conversations.Add(convo);
			conversationEventsMap[who.Name] = events;

			NewConversation.SafeInvoke(this, new NewConversationEventArgs(convo));

			return convo;
		}

		public void DeleteConversation(Conversation convo)
		{
			conversations.Remove(convo);
			conversationEventsMap.Remove(convo.Name);
		}
	}

	public class UserEventArgs : EventArgs
	{
		public UserEventArgs(IUser user)
		{
			User = user;
		}

		public readonly IUser User;
	}

	public class ChatReceivedEventArgs : EventArgs
	{
		public ChatReceivedEventArgs(ChatMessage message)
		{
			Message = message;
		}

		public readonly ChatMessage Message;
	}

	public class UserTypingEventArgs : EventArgs
	{
		public UserTypingEventArgs(IUser user, bool starting)
		{
			User = user;
			Starting = starting;
		}

		public readonly IUser User;
		public readonly bool Starting;
	}

	class ConversationEvents
	{
		public event EventHandler<UserEventArgs> UserAdded;
		public event EventHandler<UserEventArgs> UserChanged;
		public event EventHandler<UserEventArgs> UserRemoved;
		public event EventHandler<ChatReceivedEventArgs> ChatReceived;
		public event EventHandler<UserTypingEventArgs> UserTyping;

		public void RaiseUserAdded(UserEventArgs e)
		{
			UserAdded.SafeInvoke(this, e);
		}

		public void RaiseUserChanged(UserEventArgs e)
		{
			UserChanged.SafeInvoke(this, e);
		}

		public void RaiseUserRemoved(UserEventArgs e)
		{
			UserRemoved.SafeInvoke(this, e);
		}

		public void RaiseChatReceived(ChatReceivedEventArgs e)
		{
			ChatReceived.SafeInvoke(this, e);
		}

		public void RaiseUserTyping(UserTypingEventArgs e)
		{
			UserTyping.SafeInvoke(this, e);
		}
	}
}
