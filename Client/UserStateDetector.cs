using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Threading;

namespace Client
{
	class UserBusyEventArgs : EventArgs
	{
		public UserBusyEventArgs(bool _IsBusy)
		{
			IsBusy = _IsBusy;
		}

		public readonly bool IsBusy;
	}

	class UserIdleEventArgs : EventArgs
	{
		public UserIdleEventArgs(bool _IsIdle)
		{
			IsIdle = _IsIdle;
		}

		public readonly bool IsIdle;
	}

	class UserStateDetector : IDisposable
	{
		private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		private static readonly uint AppBarNotifyMessage = 0x0400; // WM_USER

		private Window messageWindow;
		private Timer idleTimer;
		private uint idleThreshold;
		private bool isIdleEnabled = false;
		private bool isBusyEnabled = false;
		private bool isDisposed = false;

		public bool IsIdle
		{ get; private set; }

		public bool IsIdleEnabled
		{
			get { return isIdleEnabled; }
			set
			{
				isIdleEnabled = value;
				IsIdle = false;
				UpdateIdleTimer();
				if (UserIdleChanged != null)
					UserIdleChanged(this, new UserIdleEventArgs(IsIdle));
			}
		}

		public bool IsBusy
		{ get; private set; }

		public bool IsBusyEnabled
		{
			get { return isBusyEnabled; }
			set
			{
				isBusyEnabled = value;
				IsBusy = false;
				if (UserBusyChanged != null)
					UserBusyChanged(this, new UserBusyEventArgs(IsBusy));
			}
		}

		public double IdleTimeThreshold
		{
			get
			{
				return Convert.ToDouble(idleThreshold) / 1000;
			}
			set
			{
				idleThreshold = Convert.ToUInt32(value * 1000);
				UpdateIdleTimer();
			}
		}

		public event EventHandler<UserBusyEventArgs> UserBusyChanged;
		public event EventHandler<UserIdleEventArgs> UserIdleChanged;

		public UserStateDetector()
		{
			idleTimer = new Timer((state) => { UpdateIdleTimer(); });

			messageWindow = new Window()
			{
				ShowActivated = false,
				ShowInTaskbar = false,
				IsHitTestVisible = false,
				Width = 0,
				Height = 0,
			};
			messageWindow.Loaded += MessageWindowLoaded;
			messageWindow.Show();
		}

		private void MessageWindowLoaded(object sender, RoutedEventArgs e)
		{
			HwndSource source = HwndSource.FromVisual(messageWindow) as HwndSource;

			if (source != null)
			{
				NativeMethods.SetParent(source.Handle, NativeMethods.HWND_MESSAGE);

				var abd = NativeMethods.APPBARDATA.Default;
				abd.hWnd = source.Handle;
				abd.uCallbackMessage = AppBarNotifyMessage;

				NativeMethods.SHAppBarMessage((uint) NativeMethods.ABMsg.ABM_NEW, ref abd);

				source.AddHook(MessageWindowHook);
			}
		}

		private IntPtr MessageWindowHook(IntPtr hwnd, int Message, IntPtr wParam, IntPtr lParam, ref bool Handled)
		{
			if (IsBusyEnabled && Message == AppBarNotifyMessage)
			{
				switch ((NativeMethods.ABNotify) wParam)
				{
				case NativeMethods.ABNotify.ABN_FULLSCREENAPP:
					{
						IsBusy = lParam != IntPtr.Zero;
						if (UserBusyChanged != null)
							UserBusyChanged.Invoke(this, new UserBusyEventArgs(IsBusy));
					}
					break;
				}
			}

			return IntPtr.Zero;
		}

		private void UpdateIdleTimer()
		{
			if (!IsIdleEnabled)
			{
				idleTimer.Change(Timeout.Infinite, Timeout.Infinite);
				return;
			}

			var lastInput = NativeMethods.LASTINPUTINFO.Default;
			if (!NativeMethods.GetLastInputInfo(ref lastInput))
			{
				log.Warn("GetLastInputInfo failed");
				return;
			}

			uint ticksSinceLastInput = (uint) Environment.TickCount - lastInput.dwTime;

			if (!IsIdle)
			{
				long MSToGo = idleThreshold - ticksSinceLastInput;
				if (MSToGo < 0)
				{
					IsIdle = true;
					if (UserIdleChanged != null)
						UserIdleChanged(this, new UserIdleEventArgs(true));

					idleTimer.Change(5000, 5000);
				}
				else
					idleTimer.Change(MSToGo + 1000, Timeout.Infinite);
			}
			else
			{
				if (ticksSinceLastInput < idleThreshold)
				{
					IsIdle = false;
					if (UserIdleChanged != null)
						UserIdleChanged(this, new UserIdleEventArgs(false));

					UpdateIdleTimer();
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (isDisposed)
				return;
			isDisposed = true;
			if (disposing)
			{
				idleTimer.Dispose();
				messageWindow.Close();
			}
		}
	}
}
