using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Protocol
{
	public class ConversationManager
	{
		readonly ChatClient client;
		readonly List<Conversation> conversations = new List<Conversation>();
		readonly Dictionary<string, ConversationEvents> conversationEventsMap = new Dictionary<string, ConversationEvents>();

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

			conversationEventsMap[e.User.Name].RaiseChatReceived(_e);
		}

		void client_GroupChat(object sender, GroupChatEventArgs e)
		{
			var _e = new ChatReceivedEventArgs(new ChatMessage(e.User, e.Font, e.Body, e.Timestamp));

			conversationEventsMap[e.Group.Name].RaiseChatReceived(_e);
		}

		void client_UserDetailsChange(object sender, UserDetailsEventArgs e)
		{
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
						conversationEventsMap[convo.Name].RaiseUserAdded(new UserAddedEventArgs(member));
				}
				foreach (var member in oldMembers)
				{
					if (!group.Members.Contains(member))
						conversationEventsMap[convo.Name].RaiseUserRemoved(new UserRemovedEventArgs(member));
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

			return convo;
		}
	}

	public class UserAddedEventArgs : EventArgs
	{
		public UserAddedEventArgs(IUser user)
		{
			User = user;
		}

		public readonly IUser User;
	}

	public class UserRemovedEventArgs : EventArgs
	{
		public UserRemovedEventArgs(IUser user)
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
		public event EventHandler<UserAddedEventArgs> UserAdded;
		public event EventHandler<UserRemovedEventArgs> UserRemoved;
		public event EventHandler<ChatReceivedEventArgs> ChatReceived;
		public event EventHandler<UserTypingEventArgs> UserTyping;

		public void RaiseUserAdded(UserAddedEventArgs e)
		{
			var handler = UserAdded;
			if (handler != null)
				handler.Invoke(this, e);
		}

		public void RaiseUserRemoved(UserRemovedEventArgs e)
		{
			var handler = UserRemoved;
			if (handler != null)
				handler.Invoke(this, e);
		}

		public void RaiseChatReceived(ChatReceivedEventArgs e)
		{
			var handler = ChatReceived;
			if (handler != null)
				handler.Invoke(this, e);
		}

		public void RaiseUserTyping(UserTypingEventArgs e)
		{
			var handler = UserTyping;
			if (handler != null)
				handler.Invoke(this, e);
		}
	}
}
