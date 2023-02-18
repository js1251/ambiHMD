﻿using System;
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
            LedsPerEye = 6; // TODO: read from settings
            HMDPreview.Resize(ControlsGrid.ActualWidth);
        }

        private async void PickerButton_Click(object sender, RoutedEventArgs e) {
            HMDPreview.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
            await HMDPreview.StartPickerCaptureAsync();
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process is null) {
                return;
            }

            HMDPreview.StopCapture();
            MonitorComboBox.SelectedIndex = -1;
            var hwnd = process.MainWindowHandle;
            try {
                HMDPreview.StartHwndCapture(hwnd);
            } catch(Exception) {
                Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                _processes.Remove(process);
                comboBox.SelectedIndex = -1;
            }
        }

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var monitor = (MonitorInfo)comboBox.SelectedItem;

            if (monitor is null) {
                return;
            }

            HMDPreview.StopCapture();
            WindowComboBox.SelectedIndex = -1;
            var hmon = monitor.Hmon;
            try {
                HMDPreview.StartHmonCapture(hmon);
            } catch(Exception) {
                Debug.WriteLine($"Hmon 0x{hmon.ToInt32():X8} is not valid for capture!");
                _monitors.Remove(monitor);
                comboBox.SelectedIndex = -1;
            }
        }

        private void BlurSlider_OnValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            var blur = e.NewValue / ((Slider)sender).Maximum;
            HMDPreview.BlurPercentage = (float)blur;
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
            MonitorComboBox.SelectedIndex = -1;
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
                //PrimaryMonitorButton.IsEnabled = false;
            }
        }

        #endregion Capture Helpers

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}