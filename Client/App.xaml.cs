using Client.Protocol;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using FontStyle = Client.Protocol.FontStyle;
using Image = System.Drawing.Image;

namespace Client
{
	public struct EmoticonFrame
	{
		public BitmapImage Image;
		public int Delay;
	}

	public class Emoticon
	{
		public string Name;
		public string Shortcut;
		public Image Image;
		public EmoticonFrame[] Frames;
	}

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
		static readonly Dictionary<UserStatus, SolidColorBrush> statusBrushMap = new Dictionary<UserStatus, SolidColorBrush>();
		static readonly Dictionary<Color, SolidColorBrush> brushCache = new Dictionary<Color, SolidColorBrush>();
		Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>();


		FontOptions clientFont;

		public new static App Current
		{ get { return Application.Current as App; } }

		public static ConnectionManager ConnectionManager
		{ get; set; }

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
			log.Fatal("Unhandled exception on Dispatcher thread", e.Exception);

#if DEBUG
			System.Diagnostics.Debugger.Break();
#endif
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			log.Info("========= Application Startup =========");

			LoadEmoticons();

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

		public static SolidColorBrush GetBrush(Color color)
		{
			SolidColorBrush brush;
			if (!brushCache.TryGetValue(color, out brush))
			{
				brush = new SolidColorBrush(color);
				brush.Freeze();

				brushCache[color] = brush;
			}

			return brush;
		}

		public List<KeyValuePair<int, Emoticon>> SearchForEmoticons(string text)
		{
			// Eventually, use Aho-Corasicks string searching method
			var emoteList = new List<KeyValuePair<int, Emoticon>>();

			foreach (var emote in emoticons.Values)
			{
				int index = text.IndexOf(emote.Shortcut);
				while (index != -1)
				{
					emoteList.Add(new KeyValuePair<int, Emoticon>(index, emote));

					index = text.IndexOf(emote.Shortcut, index + emote.Shortcut.Length);
				}
			}

			return new List<KeyValuePair<int, Emoticon>>(emoteList.OrderBy(_ => _.Key));
		}

		public Emoticon GetEmoticon(string shortcut)
		{
			Emoticon emote;
			if (emoticons.TryGetValue(shortcut, out emote))
				return emote;
			return null;
		}

		void LoadEmoticons()
		{
			emoticons.Clear();

			try
			{
				using (var archive = ZipFile.OpenRead("Emotes.zip"))
				{
					var entry = archive.GetEntry("index.xml");
					if (entry == null)
						return;

					using (var archiveStream = entry.Open())
					{
						var document = new XmlDocument();
						document.Load(archiveStream);

						foreach (var element in document.SelectNodes("/Emotes/Emote").Cast<XmlNode>())
						{
							var _name = element.Attributes["Name"];
							var _shortcut = element.Attributes["Shortcut"];
							var _imagePath = element.Attributes["ImagePath"];
							if (_name != null && _shortcut != null && _imagePath != null)
							{
								var emote = new Emoticon();
								emote.Name = _name.Value;

								if (!emoticons.ContainsKey(emote.Name))
								{
									emote.Shortcut = _shortcut.Value;
									var image = Image.FromStream(archive.GetEntry(_imagePath.Value).Open());
									emote.Image = image;

									const int PropertyTagFrameDelay = 0x5100;
									using (var memory = new MemoryStream())
									{
										int frameCount = image.GetFrameCount(FrameDimension.Time);
										emote.Frames = new EmoticonFrame[frameCount];
										for (int i = 0; i < frameCount; ++i)
										{
											emote.Frames[i] = new EmoticonFrame();
											memory.Position = 0;
											image.SelectActiveFrame(FrameDimension.Time, i);
											image.Save(memory, ImageFormat.Gif);
											memory.Position = 0;

											int delay = 10 * BitConverter.ToInt32(image.GetPropertyItem(PropertyTagFrameDelay).Value, 0);
											if (delay == 0)
												delay = 100;
											emote.Frames[i].Delay = delay;

											var frameImage = new BitmapImage();
											frameImage.BeginInit();
											frameImage.StreamSource = memory;
											frameImage.CacheOption = BitmapCacheOption.OnLoad;
											frameImage.EndInit();
											frameImage.Freeze();

											emote.Frames[i].Image = frameImage;
										}
									}

									emoticons[emote.Shortcut] = emote;
								}
							}
						}
					}
				}
			}
			catch (FileNotFoundException)
			{ }	// Eat the exception; there's nothing we can do.
		}
	}
}
