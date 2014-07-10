using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Message = Newtonsoft.Json.Linq.JObject;

namespace Client.Protocol
{
	public enum ResultCode
	{
		// Codes in the 100s are client-defined for client use only
		NotSent = 100,
		NoReply = 101,
		BadMessage = 102,

		// 200s are for success
		OK = 200,
		Created = 201,
		Accepted = 202,
		NoContent = 204,
		ResetContent = 205,
		PartialContent = 206,

		// 300s are for client mess-ups
		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,

		// 500s are for server mess-ups
		InternalError = 500,
		NotImplemented = 501,
		ServiceUnavailable = 503,
		VersionUnsupported = 505,
	}

	public class MessageEventArgs : EventArgs
	{
		public MessageEventArgs(Message message)
		{
			MessageName = message["message"].ToString();
			Message = message;
		}

		public readonly string MessageName;
		public readonly Message Message;
	}

	public class StreamErrorEventArgs : EventArgs
	{
		public StreamErrorEventArgs(Exception ex)
		{
			Exception = ex;
		}

		public readonly Exception Exception;
	}

	class Network : IDisposable
	{
		static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public delegate void MessageReplyCallback(string messageName, ReplyResult result, Message message);

		public enum ReplyResult
		{
			Success,
			Fail,
			Expired,
		}

		struct MessageCallbackInfo
		{
			public string MessageName;
			public MessageReplyCallback Callback;
			public int ExpireTick;
			public string Tag;
		}

		List<MessageCallbackInfo> outstandingCallbacks = new List<MessageCallbackInfo>();
		bool disposed = false;
		Stream source;
		StreamReader reader;
		int currentTagNumber = 0;
		volatile bool stop = false;

		public event EventHandler<MessageEventArgs> MessageReceived;
		public event EventHandler<StreamErrorEventArgs> StreamError;

		public Stream Stream
		{ get { return source; } }

		public Network(Stream source)
		{
			this.source = source;
			reader = new StreamReader(source);
		}

		public void StartProcessing()
		{
			stop = false;
			new Thread(ReadFromSource)
			{
				Name = "Chat Network Processing",
				IsBackground = true,
			}.Start();
		}

		public void StopProcessing()
		{
			stop = true;
		}

		void ReadFromSource()
		{
			int nextPruneTick = Environment.TickCount + 60 * 1000;

			while (!stop)
			{
				string rawPacket;
				try
				{
					rawPacket = reader.ReadLine();
				}
				catch (IOException ex)
				{
					StreamError.SafeInvoke(this, new StreamErrorEventArgs(ex));
					break;
				}
				catch (ObjectDisposedException)
				{
					break;
				}

				log.Debug(rawPacket);

				var packet = Message.Parse(rawPacket);

				JToken rawTag;
				bool hasTag = packet.TryGetValue("tag", out rawTag);

				string messageName = (packet["message"] ?? "").ToString();
				string replyName = (packet["reply"] ?? "").ToString();
				if (messageName != "")
				{
					MessageReceived.SafeInvoke(this, new MessageEventArgs(packet));
				}
				else if (replyName != "")
				{
					if (CheckObjectFields("reply", packet, "tag", "result", "result_message"))
					{
						string tag = packet["tag"].ToString();
						var callback = outstandingCallbacks.SingleOrDefault(_callback => _callback.Tag == tag);

						if (callback.Callback != null)
						{
							callback.Callback(callback.MessageName, ReplyResult.Success, packet);
							outstandingCallbacks.Remove(callback);
						}
					}
					else
						log.Debug("Discarding reply");
				}
				else
				{
					log.Info("Received an invalid packet: not a message or a reply");
				}


				if (Environment.TickCount > nextPruneTick)
				{
					PruneCallbacks();
					nextPruneTick += 60 * 1000;
				}
			}

			foreach (var callback in outstandingCallbacks)
			{
				callback.Callback(callback.MessageName, ReplyResult.Fail, null);
			}
			outstandingCallbacks.Clear();
		}

		public Message CreateMessage(string messageName)
		{
			var message = new Message();
			message["message"] = messageName;

			string tag;
			lock (this)
			{
				tag = currentTagNumber.ToString();
				currentTagNumber++;
			}

			message["tag"] = tag;

			return message;
		}

		public Message CreateReply(string replyName, Message message)
		{
			var reply = new Message();
			reply["reply"] = replyName;
			reply["tag"] = message["tag"];

			return message;
		}

		public void SendMessage(Message message, MessageReplyCallback replyCallback)
		{
			log.DebugFormat("Sending message \"{0}\"", message["message"]);

			if (replyCallback != null)
			{
				outstandingCallbacks.Add(new MessageCallbackInfo()
				{
					MessageName = message["message"].ToString(),
					Callback = replyCallback,
					ExpireTick = Environment.TickCount + 1000 * 10,
					Tag = message["tag"].ToString(),
				});
			}

			var rawData = Encoding.UTF8.GetBytes(message.ToString(Formatting.None, null) + "\r\n");

			// TODO: Handle the stream ending
			source.Write(rawData, 0, rawData.Length);

			log.Debug("...Done");
		}

		void PruneCallbacks()
		{
			var expiredCallbacks = new List<MessageCallbackInfo>(from callback in outstandingCallbacks
																 where callback.ExpireTick < Environment.TickCount
																 select callback);

			foreach (var callback in expiredCallbacks)
			{
				outstandingCallbacks.Remove(callback);
			}
		}
	
		// Verify that all required object fields are filled
		// Returns true if all fields were found
		public bool CheckObjectFields(string name, JToken obj, params string[] fields)
		{
			if (log.IsDebugEnabled)
			{
				var missingFields = new List<string>();
				foreach (var field in fields)
					if (obj[field] == null)
						missingFields.Add(field);

				if (missingFields.Count > 0)
				{
					log.DebugFormat("Missing required fields for object '{0}': {1}", name, string.Join(", ", missingFields));
					return false;
				}
				else
					return true;
			}
			else
			{
				foreach (var field in fields)
					if (obj[field] == null)
						return false;
				return true;
			}
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
				stop = true;
				reader.Close();
			}
		}
	}
}
