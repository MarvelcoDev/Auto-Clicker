using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using WindowsInput;

namespace Auto_clicker
{
    public partial class MainWindow : Window
    {
        private bool isRunning = false;
        private InputSimulator inputSimulator;
        private LowLevelKeyboardHook keyboardHook;
        private Thread autoClickerThread;

        public MainWindow()
        {
            InitializeComponent();
            inputSimulator = new InputSimulator();

            keyboardHook = new LowLevelKeyboardHook();
            keyboardHook.OnKeyPressed += KeyboardHook_OnKeyPressed;


            keyboardHook.HookKeyboard();
        }
        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        private void KeyboardHook_OnKeyPressed(object sender, LowLevelKeyboardHook.KeyPressedEventArgs e)
        {
            if (e.Key == Key.F8)
            {
                ToggleAutoClicker();
            }
        }

        private void StartButtonClick(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                isRunning = true;
                StartAutoClicker();
            }
        }

        private void StopButtonClick(object sender, RoutedEventArgs e)
        {
            isRunning = false;
            StopAutoClicker();
        }

        private void ToggleAutoClicker()
        {
            isRunning = !isRunning;

            if (isRunning)
            {
                StartAutoClicker();
            }
            else
            {
                StopAutoClicker();
            }
        }

        private void StopAutoClicker()
        {

            if (autoClickerThread != null && autoClickerThread.IsAlive)
            {
                autoClickerThread.Join();
            }
        }

        private void StartAutoClicker()
        {
            int intervalMilliseconds;

            if (int.TryParse(intervalTextBox.Text, out intervalMilliseconds))
            {

                autoClickerThread = new Thread(() =>
                {
                    while (isRunning)
                    {
                        Dispatcher.Invoke(() => inputSimulator.Mouse.LeftButtonClick());

                        Thread.Sleep(intervalMilliseconds);
                    }
                });

                autoClickerThread.Start();
            }
            else
            {
                MessageBox.Show("Invalid interval value. Please enter a valid number.");
                isRunning = false;
            }
        }

        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {

            keyboardHook.UnhookKeyboard();
        }

        private void MinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private class LowLevelKeyboardHook
        {
            public delegate void KeyPressedEventHandler(object sender, KeyPressedEventArgs e);
            public event KeyPressedEventHandler OnKeyPressed;

            private const int WH_KEYBOARD_LL = 13;
            private const int WM_KEYDOWN = 0x0100;
            private IntPtr hookId = IntPtr.Zero;
            private HookProc hookDelegate;

            public LowLevelKeyboardHook()
            {
                hookDelegate = HookCallback;
            }

            public void HookKeyboard()
            {
                using (var curProcess = System.Diagnostics.Process.GetCurrentProcess())
                using (var curModule = curProcess.MainModule)
                {
                    hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookDelegate, GetModuleHandle(curModule.ModuleName), 0);
                }
            }

            public void UnhookKeyboard()
            {
                UnhookWindowsHookEx(hookId);
            }

            private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
            {
                if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    OnKeyPressed?.Invoke(this, new KeyPressedEventArgs(KeyInterop.KeyFromVirtualKey(vkCode)));
                }

                return CallNextHookEx(hookId, nCode, wParam, lParam);
            }

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool UnhookWindowsHookEx(IntPtr hhk);

            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

            [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            private static extern IntPtr GetModuleHandle(string lpModuleName);

            private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

            public class KeyPressedEventArgs : EventArgs
            {
                public Key Key { get; private set; }

                public KeyPressedEventArgs(Key key)
                {
                    Key = key;
                }
            }
        }
    }
}
