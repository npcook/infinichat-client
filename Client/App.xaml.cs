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

using FontStyle = Client.Protocol.FontStyle;

namespace Client
{
	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		private FontOptions clientFont;

		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		private static readonly Dictionary<UserStatus, SolidColorBrush> statusBrushMap = new Dictionary<UserStatus, SolidColorBrush>();

		public new static App Current
		{ get { return Application.Current as App; } }

		public event EventHandler<EventArgs> FontChanged;

		public FontOptions ClientFont
		{
			get { return clientFont; }
			set
			{
				clientFont = value;
				var handler = FontChanged;
				if (handler != null)
					handler(this, new EventArgs());
			}
		}

		public App()
		{
			DispatcherUnhandledException += OnUnhandledException;
		}

		private void OnUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			System.Diagnostics.Debugger.Break();
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			string DefaultFontFamily = "Segoe UI";
			Color DefaultFontColor = Colors.Black;

			var settings = Client.Properties.Settings.Default;
			var fontFamily = settings.FontFamily;
			if (Fonts.SystemFontFamilies.FirstOrDefault(family => family.Source == fontFamily) == null)
				fontFamily = DefaultFontFamily;

			Color fontColor = DefaultFontColor;
			try
			{
				fontColor = ColorConverter.ConvertFromString(settings.FontColor) as Color? ?? Colors.Black;
			}
			catch (FormatException) // If the font color from settings is invalid, we stick with the default
			{ }

			FontStyle fontStyle = (settings.FontBold ? FontStyle.Bold : 0) | (settings.FontItalic ? FontStyle.Italic : 0) | (settings.FontUnderline ? FontStyle.Underline : 0);

			ClientFont = new FontOptions(fontFamily, fontColor, fontStyle);

			if (statusBrushMap.Count == 0)
			{
				statusBrushMap.Add(UserStatus.Available, Resources["AvailableBrush"] as SolidColorBrush);
				statusBrushMap.Add(UserStatus.Away, Resources["AwayBrush"] as SolidColorBrush);
				statusBrushMap.Add(UserStatus.Busy, Resources["BusyBrush"] as SolidColorBrush);
				statusBrushMap.Add(UserStatus.Offline, Resources["OfflineBrush"] as SolidColorBrush);
			}

			base.OnStartup(e);
		}

		protected override void OnExit(ExitEventArgs e)
		{
			var settings = Client.Properties.Settings.Default;
			settings.FontFamily = ClientFont.Family;
			settings.FontColor = ClientFont.Color.ToString();
			settings.FontBold = ClientFont.Style.HasFlag(Protocol.FontStyle.Bold);
			settings.FontItalic = ClientFont.Style.HasFlag(Protocol.FontStyle.Italic);
			settings.FontUnderline = ClientFont.Style.HasFlag(Protocol.FontStyle.Underline);
			settings.Save();

			base.OnExit(e);
		}

		public static SolidColorBrush GetUserStatusBrush(UserStatus category)
		{
			return statusBrushMap[category];
		}
	}
}
