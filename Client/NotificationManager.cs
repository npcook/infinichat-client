using Client.UI.View;
using Client.UI.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Windows.Documents;
using System.Windows.Media;

namespace Client
{
	public interface INotificationManager
	{
		TimeSpan DefaultShowTime
		{ get; }

		void CreateNotification(string title, string message, Color color, TimeSpan showTime);
		void CreateNotification(ICollection<Inline> title, ICollection<Inline> message, Color color, TimeSpan showTime);

		void CloseAllNotifications();
	}

	sealed class NotificationManager : INotificationManager, IDisposable
	{
		List<NotificationView> notifications = new List<NotificationView>();
		List<Timer> timers = new List<Timer>();
		bool disposed = false;

		public TimeSpan DefaultShowTime
		{ get { return TimeSpan.FromSeconds(7); } }

		public void CreateNotification(string title, string message, Color color, TimeSpan showTime)
		{
			CreateNotification(
				new Inline[] { new Run(title) },
				new Inline[] { new Run(message) },
				color, showTime);
		}

		public void CreateNotification(ICollection<Inline> title, ICollection<Inline> message, Color color, TimeSpan showTime)
		{
			var notification = new NotificationView(new NotificationViewModel()
			{
				Title = title,
				Message = message,
				BarBrush = App.GetBrush(color),
			});
			notifications.Add(notification);

			notification.Show();
			PlaceNotification(notification);

			var timer = new Timer(showTime.TotalMilliseconds);
			timer.Elapsed += (sender, e) =>
			{
				timer.Stop();
				timer.Dispose();
				timers.Remove(timer);

				notification.Dispatcher.BeginInvoke(new Action(() =>
				{
					notification.Close();
					notifications.Remove(notification);
				}));
			};
			timers.Add(timer);
			timer.Start();
		}

		void PlaceNotification(NotificationView notification)
		{
			if (App.Current.MainWindow != null)
			{
				var source = System.Windows.PresentationSource.FromVisual(notification);
				if (source != null)
				{
					var workArea = System.Windows.SystemParameters.WorkArea;
					var point = source.CompositionTarget.TransformToDevice.Transform(workArea.BottomRight);

					point.X -= notification.Width + 5;
					point.Y -= notifications.Count * notification.Height + 5;

					notification.Left = point.X;
					notification.Top = point.Y;
				}
			}
		}

		public void CloseAllNotifications()
		{
			foreach (var timer in timers)
				timer.Dispose();
			timers.Clear();

			foreach (var notification in notifications)
				notification.Close();
			notifications.Clear();
		}

		~NotificationManager()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			GC.SuppressFinalize(this);
			Dispose(true);
		}

		void Dispose(bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (disposing)
			{
				CloseAllNotifications();
			}
		}
	}
}
