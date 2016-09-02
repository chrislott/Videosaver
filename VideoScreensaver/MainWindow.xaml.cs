using System;
using System.Collections.Generic;
using System.IO;
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
using System.Windows.Threading;

namespace VideoScreensaver {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private bool preview;
        private Point? lastMousePosition = null;  // Workaround for "MouseMove always fires when maximized" bug.
        private int currentItem = -1;
        private List<String> videoPaths;
        private DispatcherTimer imageTimer;
        private double volume {
            get { return FullScreenMedia.Volume; }
            set {
                FullScreenMedia.Volume = Math.Max(Math.Min(value, 1), 0);
                PreferenceManager.WriteVolumeSetting(FullScreenMedia.Volume);
            }
        }

        public MainWindow(bool preview) {
            InitializeComponent();
            this.preview = preview;
            FullScreenMedia.Volume = PreferenceManager.ReadVolumeSetting();
            imageTimer = new DispatcherTimer();
            imageTimer.Tick += ImageTimerEnded;
            imageTimer.Interval = TimeSpan.FromSeconds(10);
            if (preview) {
                ShowError("When fullscreen, control volume with up/down arrows or mouse wheel.");
            }
        }

        private void ScrKeyDown(object sender, KeyEventArgs e) {
            switch (e.Key) {
                case Key.Up:
                case Key.VolumeUp:
                    volume += 0.1;
                    break;
                case Key.Down:
                case Key.VolumeDown:
                    volume -= 0.1;
                    break;
                case Key.VolumeMute:
                case Key.D0:
                    volume = 0;
                    break;
                default:
                    EndFullScreensaver();
                    break;
            }
        }

        private void ScrMouseWheel(object sender, MouseWheelEventArgs e) {
            volume += e.Delta / 1000.0;
        }

        private void ScrMouseMove(object sender, MouseEventArgs e) {
            // Workaround for bug in WPF.
            Point mousePosition = e.GetPosition(this);
            if (lastMousePosition != null && mousePosition != lastMousePosition) {
                EndFullScreensaver();
            }
            lastMousePosition = mousePosition;
        }

        private void ScrMouseDown(object sender, MouseButtonEventArgs e) {
            EndFullScreensaver();
        }
        
        // End the screensaver only if running in full screen. No-op in preview mode.
        private void EndFullScreensaver() {
            if (!preview) {
                Close();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            videoPaths = PreferenceManager.ReadVideoSettings();
            if (videoPaths.Count == 0) {
                ShowError("This screensaver needs to be configured before any video is displayed.");
            } else
            {
                NextMediaItem();
            }
        }

        private void NextMediaItem()
        {
            currentItem = (currentItem + 1) % videoPaths.Count;

            FileInfo fi = new FileInfo(videoPaths[currentItem]);
            if (fi.Extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
                fi.Extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
            {
                LoadImage(fi.FullName);
            }
            else
            {
                LoadMedia(fi.FullName);
            }
        }

        private void LoadImage(string filename)
        {
            FullScreenImage.Visibility = Visibility.Visible;
            FullScreenMedia.Visibility = Visibility.Collapsed;
            FullScreenImage.Source = new BitmapImage(new Uri(filename));
            imageTimer.Start();
        }

        private void LoadMedia(string filename)
        {
            FullScreenImage.Visibility = Visibility.Collapsed;
            FullScreenMedia.Visibility = Visibility.Visible;
            FullScreenMedia.Source = new Uri(filename);
            FullScreenMedia.Play();
        }

        private void ShowError(string errorMessage) {
            ErrorText.Text = errorMessage;
            ErrorText.Visibility = System.Windows.Visibility.Visible;
            if (preview) {
                ErrorText.FontSize = 12;
            }
        }

        private void MediaEnded(object sender, RoutedEventArgs e) {
            FullScreenMedia.Position = new TimeSpan(0);
            FullScreenMedia.Stop();
            FullScreenMedia.Source = null;
            NextMediaItem();
        }
        
        private void ImageTimerEnded(object sender, EventArgs e)
        {
            imageTimer.Stop();
            FullScreenImage.Source = null;
            NextMediaItem();
        }
    }
}
