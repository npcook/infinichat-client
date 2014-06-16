using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace Client
{
	internal sealed class NativeMethods
	{
		[DllImport("user32.dll")]
		internal static extern IntPtr SetParent(IntPtr hwnd, IntPtr hwndParent);

		internal static readonly IntPtr HWND_MESSAGE = (IntPtr) (-3);

		[DllImport("shell32.dll")]
		internal static extern IntPtr SHAppBarMessage(uint dwMessage,
		   [In] ref APPBARDATA pData);

		[StructLayout(LayoutKind.Sequential)]
		internal struct RECT
		{
			Int32 left;
			Int32 top;
			Int32 right;
			Int32 bottom;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct APPBARDATA
		{
			public static readonly int SizeOf = Marshal.SizeOf(typeof(APPBARDATA));

			public int cbSize;
			public IntPtr hWnd;
			public uint uCallbackMessage;
			public uint uEdge;
			public RECT rc;
			public int lParam;

			public static APPBARDATA Default
			{
				get
				{
					return new APPBARDATA() { cbSize = Marshal.SizeOf(typeof(APPBARDATA)) };
				}
			}
		}

		internal enum ABMsg
		{
			ABM_NEW = 0,
			ABM_REMOVE = 1,
			ABM_QUERYPOS = 2,
			ABM_SETPOS = 3,
			ABM_GETSTATE = 4,
			ABM_GETTASKBARPOS = 5,
			ABM_ACTIVATE = 6,
			ABM_GETAUTOHIDEBAR = 7,
			ABM_SETAUTOHIDEBAR = 8,
			ABM_WINDOWPOSCHANGED = 9,
			ABM_SETSTATE = 10,
		}

		internal enum ABState
		{
			ABS_MANUAL = 1,
			ABS_AUTOHIDE = 2,
			ABS_ALWAYSONTOP = 3,
			ABS_AUTOHIDEANDONTOP = 4,
		}

		internal enum ABNotify
		{
			ABN_STATECHANGE = 0,
			ABN_POSCHANGED = 1,
			ABN_FULLSCREENAPP = 2,
			ABN_WINDOWARRANGE = 3,
		}

		internal enum ABEdge
		{
			ABE_LEFT = 0,
			ABE_TOP = 1,
			ABE_RIGHT = 2,
			ABE_BOTTOM = 3,
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct LASTINPUTINFO
		{
			public int cbSize;
			public uint dwTime;

			public static LASTINPUTINFO Default
			{
				get
				{
					return new LASTINPUTINFO()
					{ cbSize = Marshal.SizeOf(typeof(LASTINPUTINFO)) };
				}
			}
		}

		[DllImport("user32.dll")]
		internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
	}
}
