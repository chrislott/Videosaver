using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;

namespace VideoScreensaver
{

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct RECT {
            public int left, top, right, bottom;
        }

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        private void OnStartup(object sender, StartupEventArgs e) {
            if (e.Args.Length > 0) {
                switch (e.Args[0].Substring(0, 2).ToLower()) {
                    case "/c":
                        // User clicked the "configure" button.
                        ConfigureScreensaver();
                        Shutdown(0);
                        return;
                    case "/p":
                        // Previewing inside of a Win32 window specified by args[1].
                        ShowInParent(new IntPtr(Convert.ToInt32(e.Args[1])));
                        return;
                }
            }
            new MainWindow(false).Show();
        }

        private void ShowInParent(IntPtr parentHwnd) {
            MainWindow previewContent = new MainWindow(true);
            WindowInteropHelper windowHelper = new WindowInteropHelper(previewContent);
            windowHelper.Owner = parentHwnd;
            previewContent.WindowState = WindowState.Normal;
            RECT parentRect;
            GetClientRect(parentHwnd, out parentRect);
            previewContent.Left = 0;
            previewContent.Top = 0;
            previewContent.Width = 0;
            previewContent.Height = 0;
            previewContent.ShowInTaskbar = false;
            previewContent.ShowActivated = false;  // Doesn't work, so we'll use SetForegroundWindow() to restore focus.
            previewContent.Cursor = Cursors.Arrow;
            previewContent.ForceCursor = false;

            IntPtr currentFocus = GetForegroundWindow();
            previewContent.Show();
            SetParent(windowHelper.Handle, parentHwnd);
            SetWindowLong(windowHelper.Handle, -16, new IntPtr(0x10000000 | 0x40000000 | 0x02000000));
            previewContent.Width = parentRect.right - parentRect.left;
            previewContent.Height = parentRect.bottom - parentRect.top;
            SetForegroundWindow(currentFocus);
        }

        private void ConfigureScreensaver() {
            List<String> videoUri = PreferenceManager.ReadVideoSettings();
            Microsoft.Win32.OpenFileDialog openDialog = new Microsoft.Win32.OpenFileDialog();
            openDialog.Multiselect = true;
            openDialog.FileName = (videoUri.Count > 0) ? videoUri[0] : "";
            openDialog.Title = "Select video files to display...";
            if (openDialog.ShowDialog() == true) {
                List<String> videos = new List<String>();
                videos.AddRange(openDialog.FileNames);
                PreferenceManager.WriteVideoSettings(videos);
            }
        }
    }
}
