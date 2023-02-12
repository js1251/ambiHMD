//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using Composition.WindowsRuntimeHelpers;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using Windows.Foundation.Metadata;
using Windows.Graphics.Capture;
using Windows.UI.Composition;
using CaptureCore;
using CompositionTarget = Windows.UI.Composition.CompositionTarget;
using ContainerVisual = Windows.UI.Composition.ContainerVisual;

namespace Ui {
    public partial class MainWindow : Window {
        public int LedsPerEye {
            get => _ledsPerEye;
            set {
                _ledsPerEye = value;
                HMDPreview.LedPerEye = value;
            }
        }

        private IntPtr _hwnd;
        private Compositor _compositor;
        private CompositionTarget _target;
        private ContainerVisual _root;

        private CaptureApplication _captureApp;
        private ObservableCollection<Process> _processes;
        private ObservableCollection<MonitorInfo> _monitors;
        private int _ledsPerEye = 6; // TODO: read from settings

        public MainWindow() {
            InitializeComponent();

#if DEBUG
            // Force graphicscapture.dll to load.
            var _ = new GraphicsCapturePicker();
#endif
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            var interopWindow = new WindowInteropHelper(this);
            _hwnd = interopWindow.Handle;

            var presentationSource = PresentationSource.FromVisual(this);
            var dpiX = 1.0;
            //var dpiY = 1.0;
            if (presentationSource != null) {
                dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
                //dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;
            }

            var controlsWidth = (float)(ControlsGrid.ActualWidth * dpiX);

            InitComposition(controlsWidth);
            InitWindowList();
            InitMonitorList();

            // SetLedAmount();
            HMDPreview.LedPerEye = LedsPerEye;
        }

        private void InitComposition(float controlsWidth) {
            // Create the compositor.
            _compositor = new Compositor();

            // Create a target for the window.
            _target = _compositor.CreateDesktopWindowTarget(_hwnd, true);

            // Attach the root visual.
            _root = _compositor.CreateContainerVisual();
            _root.RelativeSizeAdjustment = Vector2.One;
            _root.Size = new Vector2(-controlsWidth, 0);
            _root.Offset = new Vector3(controlsWidth, 0, 0);
            _target.Root = _root;

            // Setup the rest of the sample application.
            _captureApp = new CaptureApplication(_compositor);
            _root.Children.InsertAtTop(_captureApp.Visual);

            _captureApp.ColorChanged += (sender, color) => {
                var (a, r, g, b) = color;
                HMDPreview.SetLedColor(0, Color.FromArgb(a, r, g, b));
            };
        }

        private async void PickerButton_Click(object sender, RoutedEventArgs e) {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
            await StartPickerCaptureAsync();
        }

        private void WindowComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var process = (Process)comboBox.SelectedItem;

            if (process is null) {
                return;
            }

            StopCapture();
            MonitorComboBox.SelectedIndex = -1;
            var hwnd = process.MainWindowHandle;
            try {
                StartHwndCapture(hwnd);
            } catch(Exception) {
                Debug.WriteLine($"Hwnd 0x{hwnd.ToInt32():X8} is not valid for capture!");
                _processes.Remove(process);
                comboBox.SelectedIndex = -1;
            }
        }

        //private void PrimaryMonitorButton_Click(object sender, RoutedEventArgs e) {
        //    StopCapture();
        //    WindowComboBox.SelectedIndex = -1;
        //    MonitorComboBox.SelectedIndex = -1;
        //    StartPrimaryMonitorCapture();
        //}

        private void MonitorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            var comboBox = (ComboBox)sender;
            var monitor = (MonitorInfo)comboBox.SelectedItem;

            if (monitor is null) {
                return;
            }

            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            var hmon = monitor.Hmon;
            try {
                StartHmonCapture(hmon);
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

        private void StopButton_Click(object sender, RoutedEventArgs e) {
            StopCapture();
            WindowComboBox.SelectedIndex = -1;
            MonitorComboBox.SelectedIndex = -1;
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

        private async Task StartPickerCaptureAsync() {
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(_hwnd);
            var item = await picker.PickSingleItemAsync();

            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
            }
        }

        private void StartHwndCapture(IntPtr hwnd) {
            var item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
            }
        }

        private void StartHmonCapture(IntPtr hmon) {
            var item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
            }
        }

        //private void StartPrimaryMonitorCapture() {
        //    var monitor = (from m in MonitorEnumerationHelper.GetMonitors() where m.IsPrimary select m).First();
        //    StartHmonCapture(monitor.Hmon);
        //}

        private void StopCapture() {
            _captureApp.StopCapture();
        }

        #endregion Capture Helpers
    }
}