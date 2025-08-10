using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Guard.WPF.Core
{
    internal class NativeWindow
    {
        [DllImport("user32.dll")]
        internal static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean IsIconic([In] IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean SetForegroundWindow([In] IntPtr windowHandle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern Boolean ShowWindow(
            [In] IntPtr windowHandle,
            [In] ShowWindowCommand command
        );

        // https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-setwindowplacement
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool SetWindowPlacement(
            IntPtr windowHandle,
            [In] ref WINDOWPLACEMENT lpwndpl
        );

        // https://learn.microsoft.com/de-de/windows/win32/api/winuser/nf-winuser-getwindowplacement
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern bool GetWindowPlacement(
            IntPtr windowHandle,
            out WINDOWPLACEMENT lpwndpl
        );

        public enum ShowWindowCommand : int
        {
            Hide = 0x0,
            ShowNormal = 0x1,
            ShowMinimized = 0x2,
            ShowMaximized = 0x3,
            ShowNormalNotActive = 0x4,
            Minimize = 0x6,
            ShowMinimizedNotActive = 0x7,
            ShowCurrentNotActive = 0x8,
            Restore = 0x9,
            ShowDefault = 0xA,
            ForceMinimize = 0xB,
        }

        // http://www.pinvoke.net/default.aspx/Structures/RECT.html
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT(int left, int top, int right, int bottom)
        {
            public int Left = left;
            public int Top = top;
            public int Right = right;
            public int Bottom = bottom;
        }

        // https://www.pinvoke.net/default.aspx/Structures.POINT
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT(int x, int y)
        {
            public int X = x;
            public int Y = y;
        }

        // https://www.pinvoke.net/default.aspx/Structures.WINDOWPLACEMENT
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            /// <summary>
            /// The length of the structure, in bytes. Before calling the GetWindowPlacement or SetWindowPlacement functions, set this member to sizeof(WINDOWPLACEMENT).
            /// <para>
            /// GetWindowPlacement and SetWindowPlacement fail if this member is not set correctly.
            /// </para>
            /// </summary>
            public int Length;

            /// <summary>
            /// Specifies flags that control the position of the minimized window and the method by which the window is restored.
            /// </summary>
            public int Flags;

            /// <summary>
            /// The current show state of the window.
            /// </summary>
            public ShowWindowCommand ShowCmd;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is minimized.
            /// </summary>
            public POINT MinPosition;

            /// <summary>
            /// The coordinates of the window's upper-left corner when the window is maximized.
            /// </summary>
            public POINT MaxPosition;

            /// <summary>
            /// The window's coordinates when the window is in the restored position.
            /// </summary>
            public RECT NormalPosition;
        }

        public static nint WindowToNativeHandle(Window window)
        {
            return new WindowInteropHelper(window).Handle;
        }
    }
}
