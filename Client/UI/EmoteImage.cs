using System;
using System.Timers;
using System.Windows;
using System.Windows.Controls;

namespace Client.UI
{
	// Adapted from Dario's work at http://stackoverflow.com/questions/210922/how-do-i-get-an-animated-gif-to-work-in-wpf

	class EmoteImage : Image
	{
		public static readonly DependencyProperty GifSourceProperty = DependencyProperty.Register("Emote", typeof(Emoticon), typeof(EmoteImage),
			new PropertyMetadata(OnEmoteChanged));
		public static readonly DependencyProperty FrameIndexProperty = DependencyProperty.Register("FrameIndex", typeof(int), typeof(EmoteImage),
			new PropertyMetadata(-1, OnFrameIndexChanged));

		Timer frameTimer;

		public Emoticon Emote
		{
			get { return (Emoticon) GetValue(GifSourceProperty); }
			set { SetValue(GifSourceProperty, (Emoticon) value); }
		}

		public int FrameIndex
		{
			get { return (int) GetValue(FrameIndexProperty); }
			set { SetValue(FrameIndexProperty, value); }
		}

		public EmoteImage()
		{ }

		void OnTimerElapsed(object sender, ElapsedEventArgs e)
		{
			Dispatcher.Invoke(() =>
			{
				FrameIndex = (FrameIndex + 1) % Emote.Frames.Length;
				frameTimer.Interval = Emote.Frames[FrameIndex].Delay;
			});
		}

		void OnEmoteChanged(Emoticon source)
		{
			if (frameTimer != null)
			{
				frameTimer.Elapsed -= OnTimerElapsed;
				frameTimer.Stop();
				frameTimer.Dispose();
				frameTimer = null;
			}

			Width = source.Image.Width;
			Height = source.Image.Height;

			FrameIndex = 0;

			if (source.Frames.Length > 1)
			{
				frameTimer = new Timer(Emote.Frames[0].Delay);
				frameTimer.Elapsed += OnTimerElapsed;
				frameTimer.Start();
			}
		}

		void OnFrameIndexChanged(int index)
		{
			var frameImage = Emote.Frames[index].Image;
			Source = frameImage;
		}

		static void OnEmoteChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is EmoteImage)
				(d as EmoteImage).OnEmoteChanged((Emoticon) e.NewValue);
		}

		static void OnFrameIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is EmoteImage)
				(d as EmoteImage).OnFrameIndexChanged((int) e.NewValue);
		}
	}
}
