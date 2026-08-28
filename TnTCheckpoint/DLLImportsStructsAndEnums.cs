using System.Runtime.InteropServices;
using static TnTCheckpoint.TnTCheckpoint;

namespace TnTCheckpoint
{
    public class DLLImportsStructsAndEnums
    {
        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("User32.Dll")]
        public static extern long SetCursorPos(int x, int y);


        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(
            [In] uint dwFlags,
            [In] uint dx,
            [In] uint dy,
            [In] int dwData,
            [In] uint dwExtraInfo);

        public class Keyboard
        {
            [DllImport("user32.dll")]
            static extern short GetAsyncKeyState(int vKey);

            public static bool IsPressed(int key) => (GetAsyncKeyState(key) & 0x8000) != 0;
        }

        public enum MouseEvents
        {
            MOUSEEVENTF_LEFTDOWN = 0x02,
            MOUSEEVENTF_LEFTUP = 0x04,
            MOUSEEVENTF_RIGHTDOWN = 0x08,
            MOUSEEVENTF_RIGHTUP = 0x10,
            MOUSEEVENTF_WHEEL = 0x0800,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        public class CommandLayout
        {
            public bool works;
            public string activity;
            public string activitykey;
            public bool master;
            public string checkpointname;
            public string guardianname;
            public bool feats;
            public List<string> featlist;
        }
    }
}
