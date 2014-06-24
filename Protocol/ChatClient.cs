using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Windows.Media;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Client.Protocol
{
	public class ChatClient : IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private Network protocol = null;

		private SortedList<string, UserDescription> friendDescriptions = new SortedList<string, UserDescription>();
		private SortedList<string, GroupDescription> groupDescriptions = new SortedList<string, GroupDescription>();

		// All of our friends
		private SortedList<string, User> friends = new SortedList<string, User>();

		// All groups which we are in
		private SortedList<string, Group> groups = new SortedList<string, Group>();

		// All users that we have seen so far, including ghost users.
		// Every User which we create should be in this list
		private SortedList<string, User> userCache = new SortedList<string, User>();

		private bool disposed = false;

		public IUser Me
		{ get; private set; }

		public event EventHandler<UserDetailsEventArgs> UserDetailsChange;
		public event EventHandler<GroupDetailsEventArgs> GroupDetailsChange;
		public event EventHandler<UserChatEventArgs> UserChat;
		public event EventHandler<GroupChatEventArgs> GroupChat;

		public ChatClient()
		{ }

		public void Connect(Stream source)
		{
			protocol = new Network(source);
			protocol.OnMessage += HandleMessage;

			protocol.StartProcessing();
		}

		public void Disconnect()
		{
			protocol.OnMessage -= HandleMessage;
			protocol.Dispose();
			protocol = null;
		}

		void HandleMessage(object sender, MessageEventArgs e)
		{
			switch (e.MessageName)
			{
				case "detail.users":
					{
						List<User> addedUsers = new List<User>();
						List<User> changedUsers = new List<User>();
						foreach (var rawUser in e.Message["users"])
						{
							// Convert the JSON object into a User
							var description = rawUser.ToObject<UserDescription>();
							var username = description.Name;
							
							// Set the change type based on whether or not we've seen the user before
							User user;
							if (userCache.TryGetValue(username, out user))
							{
								user.Update(description);
								changedUsers.Add(user);
							}
							else
							{
								user = new User(this, description);
								userCache[username] = user;
								addedUsers.Add(user);
							}

							// Add or remove the user from our friends list
							if (user.Relation == UserRelation.Friend)
								friends[username] = user;
							else if (friends.ContainsKey(username))
								friends.Remove(username);
						}

						if (UserDetailsChange != null)
							UserDetailsChange(this, new UserDetailsEventArgs(addedUsers, changedUsers));
					}
					break;

				case "detail.groups":
					{
						List<Group> addedGroups = new List<Group>();
						List<Group> changedGroups = new List<Group>();
						foreach (var rawGroup in e.Message["groups"])
						{
							var description = rawGroup.ToObject<GroupDescription>();
							var groupname = description.Name;

							Group group;
							if (groups.TryGetValue(groupname, out group))
							{
								group.Update(description);
								changedGroups.Add(group);
							}
							else
							{
								group = new Group(this, description);
								groups[groupname] = group;
								addedGroups.Add(group);

							}
						}
						if (GroupDetailsChange != null)
							GroupDetailsChange(this, new GroupDetailsEventArgs(addedGroups, changedGroups));
					}
					break;

				case "chat.user":
					{
						var username = Convert.ToString(e.Message["from"]);
						var user = GetUser(username);

						var args = new UserChatEventArgs(
							user,
							ParseFont(e.Message["font"]),
							new UTF8Encoding(false, true).GetString(Convert.FromBase64String(Convert.ToString(e.Message["body"]))),
							DateTime.UtcNow
							);
						if (UserChat != null)
							UserChat(this, args);
					}
					break;

				case "chat.group":
					{
						var username = Convert.ToString(e.Message["from"]);
						var user = GetUser(username);
						var groupname = Convert.ToString(e.Message["via"]);
						Group group;
						if (!groups.TryGetValue(groupname, out group))
						{
							log.Warn("Got a chat message from a group we don't know about.");
							break;
						}

						var args = new GroupChatEventArgs(
							group,
							user,
							ParseFont(e.Message["font"]),
							new UTF8Encoding(false, true).GetString(Convert.FromBase64String(Convert.ToString(e.Message["body"]))),
							DateTime.UtcNow
							);
						if (GroupChat != null)
							GroupChat(this, args);
					}
					break;
			}
		}

		public void LogIn(string username, string password, EventHandler<LoginEventArgs> callback)
		{
			HashAlgorithm algorithm = SHA256.Create();
			algorithm.ComputeHash(Encoding.UTF8.GetBytes(password));

			StringBuilder sb = new StringBuilder(32);
			foreach (byte b in algorithm.Hash)
				sb.Append(b.ToString("x2"));
			string hashedPassword = sb.ToString();

			var message = protocol.CreateMessage("login");
			message["username"] = username;
			message["password"] = hashedPassword;
			message["initial_status"] = "Available";

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					Me = new User(this, reply["me"].ToObject<UserDescription>());

					callback(this, new LoginEventArgs(Convert.ToInt32(reply["result"]), Convert.ToString(reply["result_message"]), Me));
				});

			log.InfoFormat("Trying to log in as {0}", username);
		}

		public void AddFriend(string friendUserName)
		{
			var message = protocol.CreateMessage("add.user");
			message["username"] = friendUserName;

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					log.InfoFormat("Received {0}:{1}", (int) reply["result"], reply["result_message"].ToString());
				});

			log.InfoFormat("Adding friend {0}", friendUserName);
		}

		public void DetailUsers(IEnumerable<string> usernames)
		{
			var message = protocol.CreateMessage("detail.users");
			message["usernames"] = new JArray(usernames.ToArray());

			protocol.SendMessage(message, null);

			log.InfoFormat("Detailing users {0}", usernames.ToArray());
		}

		public void DetailGroups(IEnumerable<string> groupnames)
		{
			var message = protocol.CreateMessage("detail.users");
			message["groupnames"] = new JArray(groupnames.ToArray());

			protocol.SendMessage(message, null);

			log.InfoFormat("Detailing users {0}", groupnames.ToArray());
		}

		public void ListFriends()
		{
			var message = protocol.CreateMessage("list.friends");
			protocol.SendMessage(message, null);

			log.Info("Listing friends");
		}

		public void ListGroups()
		{
			var message = protocol.CreateMessage("list.groups");
			protocol.SendMessage(message, null);

			log.Info("Listing groups");
		}

		private void Chat(string messageTarget, string to, FontOptions font, string chatMessage)
		{
			string colorString = string.Format("#{0}{1}{2}", font.Color.R.ToString("x2"), font.Color.G.ToString("x2"), font.Color.B.ToString("x2"));

			var message = protocol.CreateMessage("chat." + messageTarget);
			message["to"] = to;
			message["font"] = new JObject();
			message["font"]["family"] = font.Family;
			message["font"]["color"] = colorString;
			message["font"]["style"] = (font.Style.HasFlag(FontStyle.Bold) ? "b" : "") +
				(font.Style.HasFlag(FontStyle.Italic) ? "i" : "") +
				(font.Style.HasFlag(FontStyle.Underline) ? "u" : "");
			message["body"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(chatMessage));

			protocol.SendMessage(message, null);
		}

		public void ChatUser(string username, FontOptions font, string chatMessage, DateTime timestamp)
		{
			Chat("user", username, font, chatMessage);
		}

		public void ChatGroup(string groupname, FontOptions font, string chatMessage, DateTime timestamp)
		{
			Chat("group", groupname, font, chatMessage);
		}

		public void ChangeDisplayName(string displayName)
		{
			var message = protocol.CreateMessage("me.name");
			message["display_name"] = displayName;

			protocol.SendMessage(message, null);
		}

		public void ChangeStatus(UserStatus status, EventHandler<ReplyEventArgs> callback)
		{
			var message = protocol.CreateMessage("me.status");
			message["status"] = status.ToString();

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					if (callback != null)
						callback(this, new ReplyEventArgs(Convert.ToInt32(reply["result"]), Convert.ToString(reply["result_message"])));
				});
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
				protocol.Dispose();
			}
		}

		public IUser GetUser(string username)
		{
			User user;
			if (!userCache.TryGetValue(username, out user))
			{
				user = User.CreateGhost(this, username);
				userCache.Add(username, user);
			}

			return user;
		}

		private FontOptions ParseFont(JToken rawFont)
		{
			var styleMap = new Dictionary<char, FontStyle>();
			styleMap.Add('b', FontStyle.Bold);
			styleMap.Add('i', FontStyle.Italic);
			styleMap.Add('u', FontStyle.Underline);

			string rawStyle = Convert.ToString(rawFont["style"]);
			FontStyle style = 0;
			foreach (var pair in styleMap)
			{
				if (rawStyle.Contains(pair.Key))
					style |= pair.Value;
			}

			return new FontOptions(
				Convert.ToString(rawFont["family"]),
				ColorConverter.ConvertFromString(Convert.ToString(rawFont["color"])) as Color? ?? Color.FromRgb(0, 0, 0),
				style
			);
		}
	}
}
