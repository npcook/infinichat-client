using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Client.UI
{
	
	[TemplatePart(Name = "PART_CloseButton", Type = typeof(Button))]
	public class ChatTabItem : TabItem, IDisposable
	{
		public static readonly DependencyProperty IsSignaledProperty = DependencyProperty.Register("IsSignaled", typeof(Boolean), typeof(ChatTabItem), new UIPropertyMetadata(false, new PropertyChangedCallback(IsSignaledChanged)));
		protected static readonly DependencyPropertyKey IsHighlightedKey = DependencyProperty.RegisterReadOnly("IsHighlighted", typeof(Boolean), typeof(ChatTabItem), new PropertyMetadata(false));
		public static readonly DependencyProperty IsHighlightedProperty = IsHighlightedKey.DependencyProperty;
		public static readonly RoutedEvent ClosedEvent = EventManager.RegisterRoutedEvent("Closed", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ChatTabItem));

		protected System.Timers.Timer highlightTimer = new System.Timers.Timer(1000);
		private bool isDisposed = false;

		public event RoutedEventHandler Closed
		{
			add
			{
				AddHandler(ClosedEvent, value);
			}
			remove
			{
				RemoveHandler(ClosedEvent, value);
			}
		}

		public bool IsHighlighted
		{
			get
			{
				return (bool) GetValue(IsHighlightedProperty);
			}
		}

		public bool IsSignaled
		{
			get
			{
				return (bool) GetValue(IsSignaledProperty);
			}
			set
			{
				SetValue(IsSignaledProperty, value);
			}
		}

		private static void IsSignaledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			ChatTabItem tab = d as ChatTabItem;
			if ((e.NewValue as bool?).HasValue && (e.NewValue as bool?).Value)
			{
				if (!tab.highlightTimer.Enabled)
				{
					tab.SetValue(IsHighlightedKey, true);
					tab.highlightTimer.Elapsed += tab.HighlightTimerElapsed;
					tab.highlightTimer.Start();
				}
			}
			else
			{
				if (tab.highlightTimer.Enabled)
				{
					tab.highlightTimer.Stop();
					tab.highlightTimer.Elapsed -= tab.HighlightTimerElapsed;
				}
				tab.SetValue(IsHighlightedKey, false);
			}
		}

		static ChatTabItem()
		{
			DefaultStyleKeyProperty.OverrideMetadata(typeof(ChatTabItem), new FrameworkPropertyMetadata(typeof(ChatTabItem)));
		}

		public override void OnApplyTemplate()
		{
			base.OnApplyTemplate();

			Button closeButton = GetTemplateChild("PART_CloseButton") as Button;
			if (closeButton != null)
				closeButton.Click += CloseButtonClick;
		}

		private void HighlightTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
		{
			Dispatcher.BeginInvoke(new Action(() => { SetValue(IsHighlightedKey, !IsHighlighted); }));
		}

		private void CloseButtonClick(object sender, RoutedEventArgs e)
		{
			RaiseEvent(new RoutedEventArgs(ClosedEvent));
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (isDisposed)
				return;
			isDisposed = true;

			if (isDisposing)
			{
				if (highlightTimer != null)
				{
					highlightTimer.Close();
					highlightTimer = null;
				}
			}
		}
	}
}
