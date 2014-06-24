using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
	public static class ExtensionMethods
	{
		public static void SafeInvoke<TEventArgs>(this EventHandler<TEventArgs> handler, object sender, TEventArgs e)
		{
			if (handler != null)
				handler(sender, e);
		}
	}
}
