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
using System.Diagnostics.Contracts;

namespace Client.Protocol
{
	public class ChatClient : IDisposable
	{
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		static readonly string MessageNotSentString = "Message could not be sent";
		static readonly string NoReplyString = "Did not recieve a reply";
//		static readonly string BadMessageString = "Server sent a bad message";
		static readonly FontOptions DefaultFontOptions = new FontOptions("", Color.FromRgb(0, 0, 0), FontStyle.None);

		Network protocol = null;
		bool loggedIn = false;

		// All of our friends
		SortedList<string, User> friends = new SortedList<string, User>();

		// All groups which we are in
		SortedList<string, Group> groups = new SortedList<string, Group>();

		// All users that we have seen so far, including ghost users.
		// Every User which we create should be in this list
		SortedList<string, User> userCache = new SortedList<string, User>();

		User me;
		bool disposed = false;

		public IUser Me
		{ get { VerifyLoggedIn(); return me; } }

		public IEnumerable<IUser> Friends
		{
			get
			{
				VerifyLoggedIn();
				foreach (var friend in friends.Values)
					yield return friend;
			}
		}

		public IEnumerable<IGroup> Groups
		{
			get
			{
				VerifyLoggedIn();
				foreach (var group in groups.Values)
					yield return group;
			}
		}

		public event EventHandler<UserDetailsEventArgs> UserDetailsChange;
		public event EventHandler<GroupDetailsEventArgs> GroupDetailsChange;
		public event EventHandler<UserChatEventArgs> UserChat;
		public event EventHandler<GroupChatEventArgs> GroupChat;

		public event EventHandler<StreamErrorEventArgs> StreamError;

		public ChatClient()
		{ }

		public void Connect(Stream source)
		{
			if (source == null)
				throw new ArgumentNullException("source");
			if (protocol != null)
			{
				log.Warn("A second connection is being established.  Disconnecting first...");
				Disconnect();
			}

			protocol = new Network(source);
			protocol.MessageReceived += HandleMessage;
			protocol.StreamError += OnStreamError;

			protocol.StartProcessing();
		}

		public void Disconnect()
		{
			if (protocol != null)
			{
				DoLogOut();

				protocol.MessageReceived -= HandleMessage;
				protocol.StreamError -= OnStreamError;
				protocol.StopProcessing();

				protocol.Dispose();
				protocol = null;
			}
		}

		#region Message Handling
		void HandleMessage(object sender, MessageEventArgs e)
		{
			Contract.Requires(protocol != null);
			
			switch (e.MessageName)
			{
				case "detail.users":
					HandleDetailUsers(e.Message);
					break;

				case "detail.groups":
					HandleDetailGroups(e.Message);
					break;

				case "chat.user":
					HandleChatUser(e.Message);
					break;

				case "chat.group":
					HandleChatGroup(e.Message);
					break;
			}
		}

		void HandleDetailUsers(JObject message)
		{
			List<User> addedUsers = new List<User>();
			List<User> changedUsers = new List<User>();
			foreach (var rawUser in message["users"])
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

		void HandleDetailGroups(JObject message)
		{
			List<Group> addedGroups = new List<Group>();
			List<Group> changedGroups = new List<Group>();
			foreach (var rawGroup in message["groups"])
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

		void HandleChatUser(JObject message)
		{
			var username = Convert.ToString(message["from"]);
			var user = GetUser(username);

			var args = new UserChatEventArgs(
				user,
				ParseFont(message["font"]),
				new UTF8Encoding(false, true).GetString(Convert.FromBase64String(Convert.ToString(message["body"]))),
				DateTime.Parse(message["timestamp"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
				);
			UserChat.SafeInvoke(this, args);
		}

		void HandleChatGroup(JObject message)
		{
			var username = Convert.ToString(message["from"]);
			var user = GetUser(username);
			var groupname = Convert.ToString(message["via"]);
			Group group;
			if (!groups.TryGetValue(groupname, out group))
			{
				log.Warn("Got a chat message from a group we don't know about.");
				return;
			}

			var args = new GroupChatEventArgs(
				group,
				user,
				ParseFont(message["font"]),
				new UTF8Encoding(false, true).GetString(Convert.FromBase64String(Convert.ToString(message["body"]))),
				DateTime.Parse(message["timestamp"].ToString(), null, System.Globalization.DateTimeStyles.RoundtripKind)
				);
			GroupChat.SafeInvoke(this, args);
		}
		#endregion

		void OnStreamError(object sender, StreamErrorEventArgs e)
		{
			StreamError.SafeInvoke(this, e);
		}

		public void LogIn(string username, string password, EventHandler<LoginReplyEventArgs> callback)
		{
			VerifyConnected();

			Contract.Requires(protocol != null);
			Contract.Requires(username != null);
			Contract.Requires(password != null);

			string hashedPassword;
			using (var algorithm = SHA256.Create())
			{
				algorithm.ComputeHash(Encoding.UTF8.GetBytes(password));

				var sb = new StringBuilder(32);
				foreach (byte b in algorithm.Hash)
					sb.Append(b.ToString("x2"));
				hashedPassword = sb.ToString();
			}

			var message = protocol.CreateMessage("login");
			message["username"] = username;
			message["password"] = hashedPassword;
			message["initial_status"] = UserStatus.Available.ToString();

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					loggedIn = true;

					LoginReplyEventArgs args;
					switch (result)
					{
						case Network.ReplyResult.Success:
							var resultCode = Convert.ToInt32(reply["result"]);
							var resultMessage = Convert.ToString(reply["result_message"]);
							if (protocol.CheckObjectFields("login", reply, "me"))
							{
								me = new User(this, reply["me"].ToObject<UserDescription>());
								userCache[me.Name] = me;
								loggedIn = true;

								args = new LoginReplyEventArgs(resultCode, resultMessage, Me);
							}
							else
								args = new LoginReplyEventArgs(resultCode, resultMessage, null);
							break;

						case Network.ReplyResult.Fail:
							args = new LoginReplyEventArgs((int) ResultCode.NotSent, MessageNotSentString, null);
							break;

						case Network.ReplyResult.Expired:
							args = new LoginReplyEventArgs((int) ResultCode.NoReply, NoReplyString, null);
							break;

						default:
							throw new Exception("This should not happen");
					}
					if (callback != null)
						callback(this, args);

					// Need to detail the logged-in user
					if (result == Network.ReplyResult.Success)
						UserDetailsChange.SafeInvoke(this, new UserDetailsEventArgs(new IUser[] { Me }, Enumerable.Empty<IUser>()));
				});

			log.InfoFormat("Trying to log in as {0}", username);
		}

		public void LogOut(string reason = null)
		{
			VerifyLoggedIn();

			var message = protocol.CreateMessage("logout");
			message["reason"] = reason;

			protocol.SendMessage(message, null);

			DoLogOut();
		}

		void DoLogOut()
		{
			protocol.StopProcessing();

			friends.Clear();
			groups.Clear();
			userCache.Clear();
			me = null;
			loggedIn = false;
		}

		public void AddFriend(string friendUserName)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);
			Contract.Requires(friendUserName != null);

			var message = protocol.CreateMessage("add.user");
			message["username"] = friendUserName;

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					log.InfoFormat("Received {0}:{1}", (int) reply["result"], reply["result_message"].ToString());
				});

			log.InfoFormat("Adding friend '{0}'", friendUserName);
		}

		public void DetailUsers(IEnumerable<string> usernames)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("detail.users");
			message["usernames"] = new JArray(usernames.ToArray());

			protocol.SendMessage(message, null);

			log.InfoFormat("Detailing users {0}", usernames.ToArray());
		}

		public void DetailGroups(IEnumerable<string> groupnames)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("detail.users");
			message["groupnames"] = new JArray(groupnames.ToArray());

			protocol.SendMessage(message, null);

			log.InfoFormat("Detailing users {0}", groupnames.ToArray());
		}

		public void ListFriends()
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("list.friends");
			protocol.SendMessage(message, null);

			log.Info("Listing friends");
		}

		public void ListGroups()
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("list.groups");
			protocol.SendMessage(message, null);

			log.Info("Listing groups");
		}

		private void Chat(string messageTarget, string to, FontOptions font, string chatMessage, DateTime timestamp, EventHandler<ReplyEventArgs> callback)
		{
			VerifyLoggedIn();

			string colorString = string.Format("#{0}{1}{2}", font.Color.R.ToString("x2"), font.Color.G.ToString("x2"), font.Color.B.ToString("x2"));

			var message = protocol.CreateMessage("chat." + messageTarget);
			message["to"] = to;
			message["font"] = new JObject();
			message["font"]["family"] = font.Family;
			message["font"]["color"] = colorString;
			message["font"]["style"] =
				(font.Style.HasFlag(FontStyle.Bold) ? "b" : "") +
				(font.Style.HasFlag(FontStyle.Italic) ? "i" : "") +
				(font.Style.HasFlag(FontStyle.Underline) ? "u" : "");
			message["body"] = Convert.ToBase64String(Encoding.UTF8.GetBytes(chatMessage));
			message["timestamp"] = timestamp.ToString("o");

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					ReplyEventArgs args;
					switch (result)
					{
						case Network.ReplyResult.Success:
							args = new ReplyEventArgs(Convert.ToInt32(reply["result"]), Convert.ToString(reply["result_message"]));
							break;

						case Network.ReplyResult.Fail:
							args = new ReplyEventArgs((int) ResultCode.NotSent, MessageNotSentString);
							break;

						case Network.ReplyResult.Expired:
							args = new ReplyEventArgs((int) ResultCode.NoReply, NoReplyString);
							break;

						default:
							throw new Exception("This should not happen");
					}
					if (callback != null)
						callback(this, args);
				});
		}

		public void ChatUser(string username, FontOptions font, string chatMessage, DateTime timestamp, EventHandler<ReplyEventArgs> callback)
		{
			Contract.Requires(protocol != null);

			Chat("user", username, font, chatMessage, timestamp, callback);
		}

		public void ChatGroup(string groupname, FontOptions font, string chatMessage, DateTime timestamp, EventHandler<ReplyEventArgs> callback)
		{
			Contract.Requires(protocol != null);

			Chat("group", groupname, font, chatMessage, timestamp, callback);
		}

		public void ChangeDisplayName(string displayName, EventHandler<ReplyEventArgs> callback)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("me.name");
			message["display_name"] = displayName;

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					ReplyEventArgs args;
					switch (result)
					{
						case Network.ReplyResult.Success:
							me.Update(new UserDescription()
							{
								Name = me.Name,
								DisplayName = displayName,
								Status = me.Status,
								Friend = false,
							});

							var meArray = new IUser[] { me };

							UserDetailsChange.SafeInvoke(this, new UserDetailsEventArgs(Enumerable.Empty<IUser>(), meArray));

							args = new ReplyEventArgs(Convert.ToInt32(reply["result"]), Convert.ToString(reply["result_message"]));
							break;

						case Network.ReplyResult.Fail:
							args = new ReplyEventArgs((int) ResultCode.NotSent, MessageNotSentString);
							break;

						case Network.ReplyResult.Expired:
							args = new ReplyEventArgs((int) ResultCode.NoReply, NoReplyString);
							break;

						default:
							throw new Exception("This should not happen");
					}
					callback.SafeInvoke(this, args);
				});
		}

		public void ChangeStatus(UserStatus status, EventHandler<ReplyEventArgs> callback)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			var message = protocol.CreateMessage("me.status");
			message["status"] = status.ToString();

			protocol.SendMessage(message, (messageName, result, reply) =>
				{
					ReplyEventArgs args;
					switch (result)
					{
						case Network.ReplyResult.Success:
							me.Update(new UserDescription()
							{
								Name = me.Name,
								DisplayName = me.DisplayName,
								Status = status,
								Friend = false,
							});

							var meArray = new IUser[] { me };

							UserDetailsChange.SafeInvoke(this, new UserDetailsEventArgs(Enumerable.Empty<IUser>(), meArray));

							args = new ReplyEventArgs(Convert.ToInt32(reply["result"]), Convert.ToString(reply["result_message"]));
							break;

						case Network.ReplyResult.Fail:
							args = new ReplyEventArgs((int) ResultCode.NotSent, MessageNotSentString);
							break;

						case Network.ReplyResult.Expired:
							args = new ReplyEventArgs((int) ResultCode.NoReply, NoReplyString);
							break;

						default:
							throw new Exception("This should not happen");
					}
					callback.SafeInvoke(this, args);
				});
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				if (protocol != null)
					protocol.Dispose();
			}
		}

		public IUser GetUser(string username)
		{
			VerifyLoggedIn();

			Contract.Requires(protocol != null);

			User user;
			if (!userCache.TryGetValue(username, out user))
			{
				user = User.CreateGhost(this, username);
				userCache.Add(username, user);
			}

			return user;
		}

		FontOptions ParseFont(JToken rawFont)
		{
			if (!protocol.CheckObjectFields("font", rawFont, "family", "style", "color"))
			{
				log.Debug("Using default font options");
				return DefaultFontOptions;
			}

			var styleMap = new Dictionary<char, FontStyle>()
			{
				{'b', FontStyle.Bold},
				{'i', FontStyle.Italic},
				{'u', FontStyle.Underline},
			};

			string rawStyle = Convert.ToString(rawFont["style"]);
			FontStyle style = 0;
			foreach (var pair in styleMap)
			{
				if (rawStyle.Contains(pair.Key))
					style |= pair.Value;
			}

			var color = ColorConverter.ConvertFromString(Convert.ToString(rawFont["color"])) as Color? ?? DefaultFontOptions.Color;

			return new FontOptions(
				Convert.ToString(rawFont["family"]),
				color,
				style
			);
		}

		void VerifyConnected()
		{
			if (protocol == null)
				throw new InvalidOperationException("Client must be Connect-ed to a stream first");
		}

		void VerifyLoggedIn()
		{
			VerifyConnected();
			if (!loggedIn)
				throw new InvalidOperationException("Client must be successfully logged-in first");
		}
	}
}
