using System.Windows;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace Autoclicker
{
    public partial class MainWindow : Window
    {
        private bool isClicking = false;
        private Thread clickerThread;
        private string hotkeyText = "F6";
        private const int HOTKEY_ID = 9000;

        [DllImport("user32.dll")]
        static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll")]
        static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            RegisterHotKey(handle, HOTKEY_ID, 0, (uint)KeyInterop.VirtualKeyFromKey(Key.F6));
            HwndSource source = PresentationSource.FromVisual(this) as HwndSource;
            source.AddHook(HwndHook);
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, HOTKEY_ID);
        }

        private void btnStart_Click(object sender, RoutedEventArgs e)
        {
            if (!isClicking)
            {
                isClicking = true;
                txtStatus.Text = "Autoclicker is running.";
                int cps;
                if (int.TryParse(txtCPS.Text, out cps))
                {
                    clickerThread = new Thread(() => PerformClicking(cps));
                    clickerThread.Start();
                }
            }
        }

        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            if (isClicking)
            {
                isClicking = false;
                txtStatus.Text = "Autoclicker is stopped.";
                if (clickerThread != null && clickerThread.IsAlive)
                {
                    clickerThread.Join();
                }
            }
        }

        private void btnSaveSettings_Click(object sender, RoutedEventArgs e)
        {
            hotkeyText = txtHotkey.Text;
            IntPtr handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            UnregisterHotKey(handle, HOTKEY_ID);
            Key key = (Key)Enum.Parse(typeof(Key), hotkeyText);
            RegisterHotKey(handle, HOTKEY_ID, 0, (uint)KeyInterop.VirtualKeyFromKey(key));
        }

        private void PerformClicking(int cps)
        {
            while (isClicking)
            {
                Dispatcher.Invoke(() =>
                {
                    POINT cursorPosition;
                    GetCursorPos(out cursorPosition);
                    mouse_event(MOUSEEVENTF_LEFTDOWN, cursorPosition.X, cursorPosition.Y, 0, 0);
                    mouse_event(MOUSEEVENTF_LEFTUP, cursorPosition.X, cursorPosition.Y, 0, 0);
                });
                Thread.Sleep(1000 / cps);

                if (!isClicking)
                {
                    break;
                }
            }
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            switch (msg)
            {
                case WM_HOTKEY:
                    if (wParam.ToInt32() == HOTKEY_ID)
                    {
                        if (isClicking)
                        {
                            btnStop_Click(null, null);
                        }
                        else
                        {
                            btnStart_Click(null, null);
                        }
                        handled = true;
                    }
                    break;
            }
            return IntPtr.Zero;
        }
    }
}