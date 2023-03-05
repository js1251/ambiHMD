using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;

namespace Ui {
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public int LedsPerEye {
            get => _ledsPerEye;
            set {
                _ledsPerEye = value;
                HMDPreview.LedPerEye = value;

                MaxVerticalSweep = 2f / value;
                OnPropertyChanged();
            }
        }

        public int ComPort {
            get => _comPort;
            set {
                _comPort = value;
                OnPropertyChanged();
            }
        }

        public bool IsConnected {
            get => _isConnected;
            set {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ConnectionColor));
            }
        }

        public SolidColorBrush ConnectionColor => IsConnected ? Brushes.Green : Brushes.Red;

        public bool ShowLedValues {
            get => _showLedValues;
            set {
                _showLedValues = value;
                HMDPreview.ShowColorValue = value;
                OnPropertyChanged();
            }
        }

        public float PreviewBlur {
            get => _previewBlur;
            set {
                _previewBlur = value;
                HMDPreview.BlurPercentage = value;
                OnPropertyChanged();
            }
        }

        public int LedBrightness {
            get => _ledBrightness;
            set {
                HMDPreview.Brightness = value;
                _ledBrightness = value;
                OnPropertyChanged();
            }
        }

        public float HorizontalSweep {
            get => _horizontalSweep;
            set {
                _horizontalSweep = value;
                HMDPreview.HorizontalSweep = value;
                OnPropertyChanged();
            }
        }

        public float VerticalSweep {
            get => _verticalSweep;
            set {
                _verticalSweep = value;
                HMDPreview.VerticalSweep = value;
                OnPropertyChanged();
            }
        }

        public float MaxVerticalSweep {
            get => _maxVerticalSweep;
            set {
                var relative = _verticalSweep / _maxVerticalSweep;
                _maxVerticalSweep = value;
                OnPropertyChanged();

                VerticalSweep = relative * value;
            }
        }

        public bool ShowSampleArea {
            get => _showSampleArea;
            set {
                _showSampleArea = value;
                HMDPreview.ShowSampleAreas = value;
                OnPropertyChanged();
            }
        }

        public float Gamma {
            get => _gamma;
            set {
                _gamma = value;
                HMDPreview.GammaCorrection = value;
                OnPropertyChanged();
            }
        }

        public int Smoothing {
            get => _smoothing;
            set {
                _smoothing = value;
                HMDPreview.Smoothing = value;
                OnPropertyChanged();
            }
        }

        public float Luminance {
            get => _luminance;
            set {
                _luminance = value;
                HMDPreview.LuminanceCorrection = value;
                OnPropertyChanged();
            }
        }

        #region Backing Fields

        private int _comPort;
        private int _ledsPerEye;
        private bool _showLedValues;
        private float _previewBlur;
        private float _verticalSweep;
        private float _maxVerticalSweep;
        private float _horizontalSweep;
        private int _ledBrightness;
        private bool _showSampleArea;
        private float _gamma;
        private int _smoothing;
        private float _luminance;

        #endregion Backing Fields

        private ObservableCollection<Process> _processes;
        private ObservableCollection<MonitorInfo> _monitors;
        private bool _isConnected;

        public MainWindow() {
            InitializeComponent();
#if DEBUG
            // Force graphicscapture.dll to load.
            var _ = new GraphicsCapturePicker();
#endif
            Closing += Save;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            InitWindowList();
            InitMonitorList();

            HMDPreview.Window_Loaded(sender, e);

            Load();

            HMDPreview.Resize(ControlsGrid.ActualWidth);
        }

        private void Load() {
            ComPort = Properties.Settings.Default.ComPort;
            LedsPerEye = Properties.Settings.Default.LedsPerEye;
            ShowLedValues = Properties.Settings.Default.ShowLedValues;
            LedBrightness = Properties.Settings.Default.LedBrightness;
            PreviewBlur = Properties.Settings.Default.PreviewBlur;
            Gamma = Properties.Settings.Default.GammaCorrection;
            Luminance = Properties.Settings.Default.LuminanceCorrection;
            Smoothing = Properties.Settings.Default.InputSmoothing;
            ShowSampleArea = Properties.Settings.Default.ShowSampleAreas;
            VerticalSweep = Properties.Settings.Default.VerticalSweep;
            HorizontalSweep = Properties.Settings.Default.HorizontalSweep;
        }

        private void Save(object _, CancelEventArgs __) {
            Properties.Settings.Default.ComPort = ComPort;
            Properties.Settings.Default.LedsPerEye = LedsPerEye;
            Properties.Settings.Default.ShowLedValues = ShowLedValues;
            Properties.Settings.Default.LedBrightness = LedBrightness;
            Properties.Settings.Default.PreviewBlur = PreviewBlur;
            Properties.Settings.Default.GammaCorrection = Gamma;
            Properties.Settings.Default.LuminanceCorrection = Luminance;
            Properties.Settings.Default.InputSmoothing = Smoothing;
            Properties.Settings.Default.ShowSampleAreas = ShowSampleArea;
            Properties.Settings.Default.VerticalSweep = VerticalSweep;
            Properties.Settings.Default.HorizontalSweep = HorizontalSweep;
            Properties.Settings.Default.Save();
        }

        private async void PickerButton_Click(object sender, RoutedEventArgs e) {
            HMDPreview.StopCapture();

            WindowComboBox.SelectedIndex = -1;
            WindowComboPlaceholder.Visibility = Visibility.Visible;

            MonitorComboBox.SelectedIndex = -1;
            MonitorComboPlaceholder.Visibility = Visibility.Visible;

            var item = await HMDPreview.StartPickerCaptureAsync();
            SetWindowTitle(item != null ? item.DisplayName : "");

            // TODO: show display name or process name in comboboxes
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process is null) {
                return;
            }

            HMDPreview.StopCapture();
            var hwnd = process.MainWindowHandle;
            try {
                HMDPreview.StartHwndCapture(hwnd);
                WindowComboPlaceholder.Visibility = Visibility.Hidden;

                MonitorComboBox.SelectedIndex = -1;
                MonitorComboPlaceholder.Visibility = Visibility.Visible;

                SetWindowTitle(process.MainWindowTitle);
            } catch(Exception) {
                Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                _processes.Remove(process);
                comboBox.SelectedIndex = -1;
                WindowComboPlaceholder.Visibility = Visibility.Visible;

                SetWindowTitle();
            }
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var monitor = (MonitorInfo)comboBox.SelectedItem;

            if (monitor is null) {
                return;
            }

            HMDPreview.StopCapture();
            var hmon = monitor.Hmon;
            try {
                HMDPreview.StartHmonCapture(hmon);
                MonitorComboPlaceholder.Visibility = Visibility.Hidden;

                WindowComboBox.SelectedIndex = -1;
                WindowComboPlaceholder.Visibility = Visibility.Visible;

                SetWindowTitle(monitor.DeviceName);
            } catch(Exception) {
                Debug.WriteLine($"Hmon 0x{hmon.ToInt32():X8} is not valid for capture!");
                _monitors.Remove(monitor);
                comboBox.SelectedIndex = -1;
                MonitorComboPlaceholder.Visibility = Visibility.Visible;

                SetWindowTitle();
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            HMDPreview.StopCapture();

            WindowComboBox.SelectedIndex = -1;
            WindowComboPlaceholder.Visibility = Visibility.Visible;

            MonitorComboBox.SelectedIndex = -1;
            MonitorComboPlaceholder.Visibility = Visibility.Visible;

            SetWindowTitle();
        }

        private void Settings_OnToggle(object sender, RoutedEventArgs e) {
            var isVisible = ControlsGrid.Visibility == Visibility.Visible;
            var hideButton = (Button)sender;

            hideButton.Content = isVisible ? "▸" : "◂";
            ControlsGrid.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;
            HMDPreview.Resize(ControlsGrid.ActualWidth);
        }

        private void ConnectButton_OnClick(object _, RoutedEventArgs __) {
            try {
                HMDPreview.ComPort = ComPort;
                IsConnected = true;
            } catch(Exception e) {
                IsConnected = false;
                MessageBox.Show(e.Message);
            }
        }

        #region Capture Helpers

        private void InitWindowList() {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8)) {
                var processesWithWindows = from p in Process.GetProcesses()
                    where !string.IsNullOrWhiteSpace(p.MainWindowTitle) &&
                          WindowEnumerationHelper.IsWindowValidForCapture(p.MainWindowHandle)
                    select p;
                _processes = new ObservableCollection<Process>(processesWithWindows);
                WindowComboBox.ItemsSource = _processes;
            } else {
                WindowComboBox.IsEnabled = false;
            }
        }

        private void InitMonitorList() {
            if (ApiInformation.IsApiContractPresent(typeof(Windows.Foundation.UniversalApiContract).FullName, 8)) {
                _monitors = new ObservableCollection<MonitorInfo>(MonitorEnumerationHelper.GetMonitors());
                MonitorComboBox.ItemsSource = _monitors;
            } else {
                MonitorComboBox.IsEnabled = false;
            }
        }

        private void SetWindowTitle(string title = "") {
            Title = "ambiHMD" + (string.IsNullOrEmpty(title) ? "" : " | " + title);
        }

        #endregion Capture Helpers

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}