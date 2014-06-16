using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using log4net;
using Client.Protocol;
using System.Windows.Media;

namespace Client
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Dictionary<OnlineStatusCategory, SolidColorBrush> statusBrushMap = new Dictionary<OnlineStatusCategory, SolidColorBrush>();

		public new static App Current
		{ get { return Application.Current as App; } }

		static App()
		{
			statusBrushMap.Add(OnlineStatusCategory.Available, new SolidColorBrush(Color.FromRgb(27, 195, 63)));
			statusBrushMap.Add(OnlineStatusCategory.Away, new SolidColorBrush(Color.FromRgb(228, 192, 62)));
			statusBrushMap.Add(OnlineStatusCategory.Busy, new SolidColorBrush(Color.FromRgb(195, 55, 4)));
			statusBrushMap.Add(OnlineStatusCategory.Offline, new SolidColorBrush(Color.FromRgb(180, 180, 180)));

			foreach (var brush in statusBrushMap.Values)
			{
				brush.Freeze();
			}
		}

		public App()
		{
		}

		static public SolidColorBrush GetUserStatusBrush(OnlineStatusCategory category)
		{
			return statusBrushMap[category];
		}
	}
}
