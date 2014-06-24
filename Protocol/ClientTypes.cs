using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace Client.Protocol
{
	#region Enumerations
	public enum UserRelation
	{
		Friend,
		PendingFriend,
		Me,
		None,
	}

	public enum UserStatus
	{
		Available,
		Away,
		Busy,
		Offline,
		Unknown,
	}

	[Flags]
	public enum FontStyle
	{
		None = 0,
		Bold,
		Italic,
		Underline,
	}
	#endregion

	public interface IContact
	{
		event EventHandler<ChatEventArgs> Chatted;
		event EventHandler<EventArgs> Changed;

		string Name
		{ get; }

		string DisplayName
		{ get; }
	}

	public interface IUser : IContact
	{
		UserStatus Status
		{ get; }

		UserRelation Relation
		{ get; }
	}

	public interface IGroup : IContact
	{
		event EventHandler<UserAddedEventArgs> UserAdded;
		event EventHandler<UserRemovedEventArgs> UserRemoved;

		ICollection<IUser> Members
		{ get; }

		bool Joined
		{ get; }
	}

	public class FontOptions
	{
		public FontOptions()
		{ }

		public FontOptions(string family, Color color, FontStyle style)
		{
			Family = family;
			Color = color;
			Style = style;
		}

		public string Family;
		public Color Color;
		public FontStyle Style;
	}

	class User : IUser, IDisposable
	{
		ChatClient client;
		bool disposed = false;

		public event EventHandler<ChatEventArgs> Chatted;
		public event EventHandler<EventArgs> Changed;

		public User(ChatClient client, UserDescription description)
		{
			this.client = client;
			Update(description);
			client.UserChat += client_UserChat;
		}

		void client_UserChat(object sender, UserChatEventArgs e)
		{
			if (e.User == this)
				Chatted.SafeInvoke(this, e);
		}

		public string Name
		{ get; private set; }

		public string DisplayName
		{ get; private set; }

		public UserStatus Status
		{ get; private set; }

		public UserRelation Relation
		{ get; private set; }

		public void Update(UserDescription description)
		{
			Name = description.Name;
			DisplayName = description.DisplayName;
			Status = description.Status;
			Relation = UserRelation.None;
			if (description.Friend ?? false)
				Relation = UserRelation.Friend;
			else if (client.Me == null)
				Relation = UserRelation.Me;

			Changed.SafeInvoke(this, new EventArgs());
		}

		public static User CreateGhost(ChatClient client, string name)
		{
			return new User(client, new UserDescription() { Name = name, DisplayName = name, Status = UserStatus.Unknown, Friend = false });
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				client.UserChat -= client_UserChat;
			}
		}
	}

	class Group : IGroup, IDisposable
	{
		ChatClient client;
		List<IUser> members;
		bool disposed;

		public event EventHandler<UserAddedEventArgs> UserAdded;
		public event EventHandler<UserRemovedEventArgs> UserRemoved;
		public event EventHandler<ChatEventArgs> Chatted;
		public event EventHandler<EventArgs> Changed;

		public Group(ChatClient client, GroupDescription description)
		{
			this.client = client;
			Update(description);
			client.GroupChat += client_GroupChat;
		}

		void client_GroupChat(object sender, GroupChatEventArgs e)
		{
			if (e.Group == this)
				Chatted.SafeInvoke(this, e);
		}

		public string Name
		{ get; private set; }

		public string DisplayName
		{ get; private set; }

		public ICollection<IUser> Members
		{ get { return members; } }

		public bool Joined
		{ get; protected set; }

		public void Update(GroupDescription description)
		{
			Name = description.Name;
			DisplayName = description.DisplayName;
			Joined = description.Member ?? false;

			var newMembers = new List<IUser>(from name in description.MemberNames select client.GetUser(name));
			if (members != null)
			{
				var oldMembers = new List<IUser>(members);
				foreach (var member in newMembers)
				{
					if (!oldMembers.Contains(member))
					{
						members.Add(member);
						UserAdded.SafeInvoke(this, new UserAddedEventArgs(member));
					}
				}
				foreach (var member in oldMembers)
				{
					if (!newMembers.Contains(member))
					{
						members.Remove(member);
						UserRemoved.SafeInvoke(this, new UserRemovedEventArgs(member));
					}
				}
			}
			else
				members = newMembers;

			Changed.SafeInvoke(this, new EventArgs());
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				client.GroupChat -= client_GroupChat;
			}
		}
	}

	[JsonObject(MemberSerialization.OptOut)]
	public struct UserDescription
	{
		[JsonProperty("username")]
		public string Name;
		[JsonProperty("display_name")]
		public string DisplayName;
		[JsonProperty("status")]
		[JsonConverter(typeof(StringEnumConverter))]
		public UserStatus Status;
		[JsonProperty("friend")]
		public bool? Friend;
	}

	[JsonObject(MemberSerialization.OptOut)]
	public struct GroupDescription
	{
		[JsonProperty("groupname")]
		public string Name;
		[JsonProperty("display_name")]
		public string DisplayName;
		[JsonProperty("members")]
		public string[] MemberNames;
		[JsonProperty("member")]
		public bool? Member;
	}

	#region Event Args
	public class UserDetailsEventArgs : EventArgs
	{
		public readonly ICollection<IUser> AddedUsers;
		public readonly ICollection<IUser> ChangedUsers;

		public UserDetailsEventArgs(IEnumerable<IUser> addedUsers, IEnumerable<IUser> changedUsers)
		{
			AddedUsers = addedUsers.ToArray();
			ChangedUsers = changedUsers.ToArray();
		}
	}

	public class GroupDetailsEventArgs : EventArgs
	{
		public readonly ICollection<IGroup> AddedGroups;
		public readonly ICollection<IGroup> ChangedGroups;

		public GroupDetailsEventArgs(IEnumerable<IGroup> addedGroups, IEnumerable<IGroup> changedGroups)
		{
			AddedGroups = addedGroups.ToArray();
			ChangedGroups = changedGroups.ToArray();
		}
	}

	public class ChatEventArgs : EventArgs
	{
		public readonly IUser User;
		public readonly FontOptions Font;
		public readonly string Body;
		public readonly DateTime Timestamp;

		public ChatEventArgs(IUser user, FontOptions font, string body, DateTime timestamp)
		{
			User = user;
			Font = font;
			Body = body;
			Timestamp = timestamp;
		}
	}

	public class UserChatEventArgs : ChatEventArgs
	{
		public UserChatEventArgs(IUser user, FontOptions font, string body, DateTime timestamp)
			: base(user, font, body, timestamp)
		{ }
	}

	public class GroupChatEventArgs : ChatEventArgs
	{
		public readonly IGroup Group;

		public GroupChatEventArgs(IGroup group, IUser user, FontOptions font, string body, DateTime timestamp)
			: base(user, font, body, timestamp)
		{
			Group = group;
		}
	}

	public class ReplyEventArgs : EventArgs
	{
		public readonly bool Success;
		public readonly int RawResult;
		public readonly ResultCode Result;
		public readonly string ResultMessage;

		public ReplyEventArgs(int result, string resultMessage)
		{
			RawResult = result;
			if (Enum.IsDefined(typeof(ResultCode), result))
				Result = (ResultCode) result;
			else
				Result = 0;
			ResultMessage = resultMessage;
			Success = (RawResult >= 200 && RawResult < 300);
		}
	}

	public class LoginEventArgs : ReplyEventArgs
	{
		public readonly IUser Me;

		public LoginEventArgs(int resultCode, string resultMessage, IUser me)
			: base(resultCode, resultMessage)
		{
			Me = me;
		}
	}
	#endregion
}
