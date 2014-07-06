using Client.Protocol;
using Client.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
	public class ConnectionManager : IDisposable
	{
		ChatClient client;
		TcpClient netClient;
		string hostname;
		int port;
		string username;
		string password;
		ConnectionDialog dialog;
		object dialogLock = new object();
		bool disposed = false;
		EventWaitHandle connectDone = new ManualResetEvent(false);
		bool connectionSucceeded;
		string connectionErrorMessage;

		public Stream Stream
		{ get { return netClient.GetStream(); } }

		public static int DefaultPort
		{ get { return 49520; } }

		public ConnectionManager(ChatClient client)
		{
			this.client = client;
		}

		public bool Connect(string hostname, int port, string username, string password)
		{
			return Connect(hostname, port, username, password, false);
		}

		private bool Connect(string hostname, int port, string username, string password, bool reconnect)
		{
			const int DialogHiddenTime = 1000;
			
			// If we are reconnecting, try to reconnect 5 times.  Otherwise, only try once
			int retryCount = reconnect ? 5 : 0;

			this.hostname = hostname;
			this.port = port;
			this.username = username;
			this.password = password;

			if (reconnect)
				dialog = new ConnectionDialog("Connection Lost", "Trying to reconnect...");
			else
				dialog = new ConnectionDialog("Infinichat", "Connecting to Infinichat...");
			dialog.Owner = App.Current.MainWindow;

			/* Create a new thread which connects to the given hostname and port and then logs in
			 * with the given username and password.  The connection thread will set the connectDone
			 * event when these operations complete.  If they complete quickly, we don't want to show
			 * a dialog. */

			var thread = new Thread(new ParameterizedThreadStart(ConnectThread));
			thread.Name = "Connection Thread";
			thread.Start(retryCount);

			// For user experience reasons, only show the dialog if connecting is taking a long time
			if (!connectDone.WaitOne(DialogHiddenTime))
			{
				dialog.ShowDialog();
				lock (dialogLock)
				{
					dialog = null;
				}
				connectDone.Set();
				thread.Join();
			}
			else
			{
				dialog.ShowDialog();
				lock (dialogLock)
				{
					dialog = null;
				}
			}
			connectDone.Reset();

			if (!connectionSucceeded)
				System.Windows.MessageBox.Show(App.Current.MainWindow, connectionErrorMessage, "Error Logging In");

			return connectionSucceeded;
		}

		public bool Reconnect()
		{
			client.Disconnect();
			return Connect(hostname, port, username, password, true);
		}

		void ConnectThread(object _maxRetryCount)
		{
			int maxRetryCount = _maxRetryCount as int? ?? 0;
			int waitTime = 500;

			netClient = new TcpClient();
			for (int i = 0; i <= maxRetryCount; ++i, waitTime *= 2)
			{
				try
				{
					netClient = new TcpClient();
					var asyncResult = netClient.BeginConnect(hostname, port, null, null);

					try
					{
						// Stop what we're doing if the connection is cancelled from the main thread
						if (EventWaitHandle.WaitAny(new WaitHandle[] { connectDone, asyncResult.AsyncWaitHandle }) == 0)
						{
							netClient.Close();
							netClient = null;

							return;
						}

						netClient.EndConnect(asyncResult);
					}
					finally
					{
						asyncResult.AsyncWaitHandle.Close();
					}

					break;
				}
				catch (SocketException ex)
				{
					if (i == maxRetryCount)
					{
						NotifyConnectionResult(false, ex.Message);
						return;
					}
					else
					{
						Thread.Sleep(waitTime);
					}
				}
			}

			// If the main thread cancels the connection, this will be set to true
			bool itsOver = false;

			client.Connect(netClient.GetStream());
			client.LogIn(username, password, (sender, e) =>
				{
					if (itsOver)
						return;

					NotifyConnectionResult(e.Success, e.ResultMessage);

					connectDone.Set();
				});

			connectDone.WaitOne();
			itsOver = true;
		}

		void NotifyConnectionResult(bool success, string errorMessage)
		{
			connectionSucceeded = success;
			connectionErrorMessage = errorMessage;
			lock (dialogLock)
			{
				if (dialog != null)
				{
					dialog.NotifyConnectionResult(success);
				}
			}
		}

		public void Disconnect()
		{
			netClient.Close();
			netClient = null;
		}

		public void Dispose()
		{
			if (disposed)
				return;
			disposed = true;

			netClient.Close();
			connectDone.Close();
		}
	}
}
