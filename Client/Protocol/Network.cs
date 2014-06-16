using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Sockets;
using System.Threading;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Message = Newtonsoft.Json.Linq.JObject;

namespace Client.Protocol
{
	public enum ResultCode
	{
		OK = 200,
		Created = 201,
		Accepted = 202,
		NoContent = 204,
		ResetContent = 205,
		PartialContent = 206,

		BadRequest = 400,
		Unauthorized = 401,
		Forbidden = 403,
		NotFound = 404,

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

	class Network : IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public delegate void MessageReplyCallback(string messageName, ReplyResult result, Message message);

		public enum ReplyResult
		{
			Success,
			Fail,
			Expired,
		}

		private struct MessageCallbackInfo
		{
			public string MessageName;
			public MessageReplyCallback Callback;
			public int ExpireTick;
			public string Tag;
		}

		private List<MessageCallbackInfo> outstandingCallbacks = new List<MessageCallbackInfo>();
		private bool disposed = false;
		private Stream source;
		private StreamReader reader;
		private int currentTagNumber = 0;
		private volatile bool stop = false;

		public event EventHandler<MessageEventArgs> OnMessage;

		public Stream Stream
		{
			get { return source; }
		}

		public Network(Stream source)
		{
//			System.Diagnostics.Debug.Assert(source != null);

			this.source = source;
			reader = new StreamReader(source);
		}

		public void StartProcessing()
		{
			stop = false;
			new Thread(ReadFromSource).Start();
		}

		public void StopProcessing()
		{
			stop = true;
		}

		private void ReadFromSource()
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
					SocketException socketEx = ex.InnerException as SocketException;
					if (socketEx != null)
					{
						switch (socketEx.SocketErrorCode)
						{
							case SocketError.ConnectionAborted:
							case SocketError.ConnectionReset:
								return;

							case SocketError.Interrupted:
								if (stop)
									return;
								else
									throw;

							default:
								throw;
						}
					}
					else
						throw;
				}
				catch (ObjectDisposedException)
				{
					return;
				}

				var packet = Message.Parse(rawPacket);

				JToken rawTag;
				bool hasTag = packet.TryGetValue("tag", out rawTag);

				string messageName = (packet["message"] ?? "").ToString();
				string replyName = (packet["reply"] ?? "").ToString();
				if (messageName != "")
				{
					if (OnMessage != null)
						OnMessage(this, new MessageEventArgs(packet));
				}
				else if (replyName != "")
				{
					string tag = rawTag.ToString();
					var replyCallbacks = from callback in outstandingCallbacks
										 where callback.Tag == tag
										 select callback;

					foreach (var callback in replyCallbacks.ToArray())
					{
						callback.Callback(callback.MessageName, ReplyResult.Success, packet);

						outstandingCallbacks.Remove(callback);
					}
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
					ExpireTick = Environment.TickCount + 1000 * 60,
					Tag = message["tag"].ToString(),
				});
			}

			var rawData = Encoding.UTF8.GetBytes(message.ToString(Formatting.None, null) + "\r\n");

			// TODO: Handle the stream ending
			source.Write(rawData, 0, rawData.Length);

			log.Debug("...Done");
		}

		private void PruneCallbacks()
		{
			var expiredCallbacks = from callback in outstandingCallbacks
								   where callback.ExpireTick < Environment.TickCount
								   select callback;

			foreach (var callback in expiredCallbacks)
			{
				outstandingCallbacks.Remove(callback);
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
