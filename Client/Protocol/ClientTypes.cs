using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

	public class Contact
	{
		public Contact(string name, string displayName)
		{
			Name = name;
			DisplayName = displayName;
		}

		public readonly string Name;
		public readonly string DisplayName;
	}

	public class User : Contact
	{
		public User(string name, string displayName, UserStatus status, UserRelation relation)
			: base(name, displayName)
		{
			Status = status;
			Relation = relation;
		}

		public readonly UserStatus Status;
		public readonly UserRelation Relation;
	}

	public class GhostUser : User
	{
		public GhostUser(string name)
			: base(name, name, UserStatus.Unknown, UserRelation.None)
		{ }
	}

	public class Group : Contact
	{
		public Group(string name, string displayName, IEnumerable<User> members, bool joined)
			: base(name, displayName)
		{
			Members = new List<User>(members);
			Joined = joined;
		}

		public bool Loaded
		{
			get { return DisplayName != null && Members != null; }
		}

		public List<User> Members
		{ get; private set; }

		public readonly bool Joined;
	}

	[JsonObject(MemberSerialization.OptOut)]
	public class UserDescription
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
	public class GroupDescription
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
		public readonly ICollection<User> AddedUsers;
		public readonly ICollection<User> ChangedUsers;

		public UserDetailsEventArgs(IEnumerable<User> addedUsers, IEnumerable<User> changedUsers)
		{
			AddedUsers = addedUsers.ToArray();
			ChangedUsers = changedUsers.ToArray();
		}
	}

	public class GroupDetailsEventArgs : EventArgs
	{
		public readonly ICollection<Group> AddedGroups;
		public readonly ICollection<Group> ChangedGroups;

		public GroupDetailsEventArgs(IEnumerable<Group> addedGroups, IEnumerable<Group> changedGroups)
		{
			AddedGroups = addedGroups.ToArray();
			ChangedGroups = changedGroups.ToArray();
		}
	}

	public class ChatEventArgs : EventArgs
	{
		public readonly User User;
		public readonly FontOptions Font;
		public readonly string Body;
		public readonly DateTime Timestamp;

		public ChatEventArgs(User user, FontOptions font, string body, DateTime timestamp)
		{
			User = user;
			Font = font;
			Body = body;
			Timestamp = timestamp;
		}
	}

	public class UserChatEventArgs : ChatEventArgs
	{
		public UserChatEventArgs(User user, FontOptions font, string body, DateTime timestamp)
			: base(user, font, body, timestamp)
		{ }
	}

	public class GroupChatEventArgs : ChatEventArgs
	{
		public readonly Group Group;

		public GroupChatEventArgs(Group group, User user, FontOptions font, string body, DateTime timestamp)
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
		public readonly User Me;

		public LoginEventArgs(int resultCode, string resultMessage, User me)
			: base(resultCode, resultMessage)
		{
			Me = me;
		}
	}
	#endregion
}
