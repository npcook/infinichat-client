using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Xml;

namespace Client
{
	class TrieNode<TValue>
	{
		public TValue Value
		{ get; protected set; }

		List<TrieNode<TValue>> children = new List<TrieNode<TValue>>();
		public ICollection<TrieNode<TValue>> Children
		{ get { return children; } }

		public object Tag
		{ get; set; }

		public TrieNode()
		{ }

		public TrieNode(TValue value)
		{
			Value = value;
		}

		public void Add(TValue child)
		{
			children.Add(new TrieNode<TValue>(child));
		}

		public bool Contains(TValue child)
		{
			return children.FirstOrDefault(_ => _.Value.Equals(child)) != null;
		}

		public void Remove(TValue child)
		{
			children.Remove(children.First(_ => _.Value.Equals(child)));
		}

		public void Remove(TrieNode<TValue> childNode)
		{
			children.Remove(childNode);
		}

		public TrieNode<TValue> this[TValue value]
		{
			get { return children.First(_ => _.Value.Equals(value)); }
		}

		public override string ToString()
		{
			return string.Format("{0}: {1} children", Value.ToString(), Children.Count);
		}
	}

	public class EmoticonManager : IDisposable
	{
		static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		const string EmotePath = "Emotes.zip";

		Dictionary<string, Emoticon> emoticons = new Dictionary<string, Emoticon>();
		TrieNode<char> emoteTrie;
		FileSystemWatcher watcher;
		Thread loadThread;

		public ICollection<Emoticon> Emoticons
		{ get { return emoticons.Values; } }

		public EmoticonManager()
		{ }

		void OnFileChanged(object sender, FileSystemEventArgs e)
		{
			if (e.ChangeType == WatcherChangeTypes.Changed)
				LoadEmoticons();
		}

		public List<KeyValuePair<int, Emoticon>> SearchForEmoticons(string text)
		{
			var emoteList = new List<KeyValuePair<int, Emoticon>>();

			int index = 0;
			while (index < text.Length)
			{
				int currentIndex = index;
				TrieNode<char> currentNode = emoteTrie;
				while (currentIndex < text.Length && currentNode.Contains(text[currentIndex]))
				{
					currentNode = currentNode[text[currentIndex]];
					currentIndex++;
				}
				
				int moveDistance = 1;
				if (currentNode.Tag != null)
				{
					var matchedEmote = currentNode.Tag as Emoticon;
					emoteList.Add(new KeyValuePair<int, Emoticon>(index, matchedEmote));

					moveDistance = matchedEmote.Shortcut.Length;
				}

				index += moveDistance;
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

		public void LoadEmoticons()
		{
			if (loadThread != null && loadThread.IsAlive)
				return;

			loadThread = new Thread(LoadEmoticonsThread)
			{
				Name = "Loading Emoticons",
				IsBackground = true,
			};
			loadThread.Start();
		}

		void LoadEmoticonsThread()
		{
			try
			{
				using (var archive = ZipFile.OpenRead(EmotePath))
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
							// Make sure the necessary attributes exist

							var _name = element.Attributes["Name"];
							var _shortcut = element.Attributes["Shortcut"];
							var _imagePath = element.Attributes["ImagePath"];
							if (_name == null || _shortcut == null || _imagePath == null)
								continue;

							var emote = new Emoticon();
							emote.Name = _name.Value;

							// NOTE: This prevents an emoticon from being updated if it changes while the application is open
							if (emoticons.ContainsKey(emote.Name))
								continue;

							emote.Shortcut = _shortcut.Value;

							var imageEntry = archive.GetEntry(_imagePath.Value);
							if (imageEntry == null)
								continue;

							var image = Image.FromStream(imageEntry.Open());
							emote.Image = image;
							emote.Frames = GetEmoticonFrames(image);

							emoticons[emote.Shortcut] = emote;
						}
					}
				}
			}
			catch (FileNotFoundException)
			{ }	// Eat the exception; there's nothing we can do.

			watcher = new FileSystemWatcher(Environment.CurrentDirectory, EmotePath);
			watcher.Changed += OnFileChanged;

			GenerateEmoticonTrie();
		}

		bool IsAnimated(Image image)
		{
			try
			{
				return image.GetFrameCount(FrameDimension.Time) > 1;
			}
			catch (ExternalException)
			{
				return false;
			}
		}

		EmoticonFrame[] GetEmoticonFrames(Image image)
		{
			using (var memory = new MemoryStream())
			{
				if (IsAnimated(image))
				{
					int frameCount = image.GetFrameCount(FrameDimension.Time);

					const int PropertyTagFrameDelay = 0x5100;
					var frameDelayProperty = image.GetPropertyItem(PropertyTagFrameDelay);
					if (frameDelayProperty != null && frameDelayProperty.Len == 4 * frameCount)
					{
						int[] frameDelays = new int[frameCount];
						for (int i = 0; i < frameDelays.Length; ++i)
						{
							// The frame delay property is in hundredths of a second, but we want milliseconds, so multiply by 10
							frameDelays[i] = 10 * BitConverter.ToInt32(frameDelayProperty.Value, 4 * i);
						}

						/* Gifs are complicated. Technically, their frames must be layered on top of one another as they are displayed.
						 * Some gifs render correctly when you do this, but some don't. They are just poorly made? So, there's a heuristic in here:
						 * If the frame delay is zero, assume that the gif author wanted the frames layered (to get more colors in each frame).
						 * Otherwise, assume that layering is not desired and clear the graphics surface before drawing on it.  We still have to
						 * draw to the graphics surface in this case because the next frame might required layering the current frame. */
						 
						using (var surface = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb))
						using (var graphics = Graphics.FromImage(surface))
						{
							var frames = new EmoticonFrame[frameCount];
							for (int i = 0; i < frameCount; ++i)
							{
								memory.Position = 0;
								image.SelectActiveFrame(FrameDimension.Time, i);

								if (frameDelays[i] != 0)
									graphics.Clear(Color.Transparent);
								graphics.DrawImage(image, 0, 0);

								surface.Save(memory, ImageFormat.Png);
								// Reset stream to the beginning so that we can read the image we just saved
								memory.Position = 0;

								frames[i] = CreateFrame(memory, Math.Max(frameDelays[i], 60));
							}

							return frames;
						}
					}
				}
				
				// If the image is not animated or some of the properties required to animate it are corrupt, just process it as a static image
				image.Save(memory, ImageFormat.Png);
				// Reset stream to the beginning so that we can read the image we just saved
				memory.Position = 0;

				return new EmoticonFrame[] { CreateFrame(memory, 0) };
			}
		}

		EmoticonFrame CreateFrame(Stream imageStream, int delay)
		{
			var frameImage = new BitmapImage();
			frameImage.BeginInit();
			frameImage.StreamSource = imageStream;
			frameImage.CacheOption = BitmapCacheOption.OnLoad;
			frameImage.EndInit();
			frameImage.Freeze();

			return new EmoticonFrame()
			{
				Image = frameImage,
				Delay = delay,
			};
		}

		void GenerateEmoticonTrie()
		{
			emoteTrie = new TrieNode<char>();
			foreach (var emote in Emoticons)
			{
				TrieNode<char> currentNode = emoteTrie;
				foreach (char c in emote.Shortcut)
				{
					if (!currentNode.Contains(c))
						currentNode.Add(c);
					currentNode = currentNode[c];
				}
				currentNode.Tag = emote;
			}
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		bool disposed = false;

		protected void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				if (watcher != null)
					watcher.Dispose();
			}
		}
	}
}
