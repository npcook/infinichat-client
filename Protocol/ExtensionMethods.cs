using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client.Protocol
{
	static class ExtensionMethods
	{
		public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs e)
		{
			if (handler != null)
				handler(sender, e);
		}

		[Conditional("DEBUG")]
		public static void CheckInvokeList<TEventArgs>(this EventHandler<TEventArgs> handler)
		{
			if (handler != null)
			{
				var invokes = handler.GetInvocationList();
				if (invokes.Length > 0)
				{
					var subscribers = string.Join(", ", from invoke in invokes select invoke.Target.ToString());

					Debug.WriteLine(string.Format("Event {0} still has these subscribers: {1}", handler.Method.Name, subscribers));
				}
			}
		}
	}
}
