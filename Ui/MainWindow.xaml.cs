using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;

namespace Ui {
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public int LedsPerEye {
            get => _ledsPerEye;
            set {
                _ledsPerEye = value;
                //_captureApp.NumberOfLedsPerEye = value;
                HMDPreview.LedPerEye = value;
                OnPropertyChanged();
            }
        }

        private ObservableCollection<Process> _processes;
        private ObservableCollection<MonitorInfo> _monitors;
        private int _ledsPerEye;

        public MainWindow() {
            InitializeComponent();
#if DEBUG
            // Force graphicscapture.dll to load.
            var _ = new GraphicsCapturePicker();
#endif
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            InitWindowList();
            InitMonitorList();

            HMDPreview.Window_Loaded(sender, e);

            LedsPerEye = 8; // TODO: read from settings
            HMDPreview.Brightness = 5; // TODO: read from settings
            HMDPreview.VerticalSweep = 0.1f;
            HMDPreview.HorizontalSweep = 1f / LedsPerEye;

            HMDPreview.Resize(ControlsGrid.ActualWidth);
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

        private void BlurSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var blur = e.NewValue / ((Slider)sender).Maximum;
            HMDPreview.BlurPercentage = (float)blur;
        }

        private void Brightness_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var brightness = e.NewValue / ((Slider)sender).Maximum;
            HMDPreview.Brightness = (float)brightness;
        }

        private void HorizontalSweep_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var sweep = e.NewValue / ((Slider)sender).Maximum;
            HMDPreview.HorizontalSweep = Math.Max((float)sweep, 0.01f);
        }

        private void VerticalSweep_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var sweep = e.NewValue / ((Slider)sender).Maximum;
            HMDPreview.VerticalSweep = Math.Max((float)sweep, 0.01f);
        }

        private void LedValues_OnToggled(object sender, RoutedEventArgs e) {
            HMDPreview.ShowColorValue = ((CheckBox)sender).IsChecked.Value;
        }

        private void SampleAreas_OnToggled(object sender, RoutedEventArgs e) {
            HMDPreview.ShowSampleAreas = ((CheckBox)sender).IsChecked.Value;
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