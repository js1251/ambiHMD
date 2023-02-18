using CaptureCore;
using System.Numerics;
using System.Windows.Controls;
using System.Windows.Media;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using CompositionTarget = Windows.UI.Composition.CompositionTarget;
using ContainerVisual = Windows.UI.Composition.ContainerVisual;
using System;
using System.Windows.Interop;
using System.Windows;
using System.Threading.Tasks;
using Windows.Graphics.Capture;

namespace Ui.customcontrols {
    public partial class HMDPreview : UserControl {
        private int _ledPerEye;

        public int LedPerEye {
            get => _ledPerEye;
            set {
                _ledPerEye = value;
                SetLedCount(value);
            }
        }

        public float BlurPercentage {
            set => SetLedBlur(value);
        }

        public float LedSize {
            set => SetLedSize(value);
        }

        public bool LedActive {
            set => SetLedActive(value);
        }

        public bool ShowColorValue {
            set => SetLedShowColorvalue(value);
        }

        public bool ShowSampleAreas {
            set => SetSampleAreas(value);
        }

        private IntPtr _hwnd;
        private Compositor _compositor;
        private CompositionTarget _target;
        private ContainerVisual _root;

        private CaptureApplication _captureApp;

        public HMDPreview() {
            InitializeComponent();
        }

        public void Resize(double width) {
            var presentationSource = PresentationSource.FromVisual(this);
            var dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
            // var dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;

            var controlsWidth = (float)(width * dpiX);

            var padding = LeftLEDs.Size + RightLEDs.Size; // led
            padding += (float) (LeftLEDs.Margin.Left + LeftLEDs.Margin.Right);
            padding += (float)(RightLEDs.Margin.Left + RightLEDs.Margin.Right);

            _root.Size = new Vector2(-controlsWidth - padding, 0);
            _root.Offset = new Vector3(controlsWidth + padding * 0.5f, 0, 0);
        }

        public void Window_Loaded(object sender, RoutedEventArgs e) {
            var window = sender as Window;
            var interopWindow = new WindowInteropHelper(window);
            _hwnd = interopWindow.Handle;
            _compositor = new Compositor();

            // Create a target for the window.
            _target = _compositor.CreateDesktopWindowTarget(_hwnd, true);

            // Attach the root visual.
            _root = _compositor.CreateContainerVisual();

            _root.RelativeSizeAdjustment = Vector2.One;
            _target.Root = _root;

            // Setup the rest of the sample application.
            _captureApp = new CaptureApplication(_compositor);
            _root.Children.InsertAtBottom(_captureApp.Visual);

            _captureApp.ColorChanged += (captureApp, index, colorData) => {
                if (LedPerEye <= 0) {
                    return;
                }

                if (index < 0 || index >= LedPerEye * 2f) {
                    return;
                }

                // BGRA
                SetLedColor(index, Color.FromArgb(colorData[3], colorData[2], colorData[1], colorData[0]));
            };
        }

        public void SetLedColor(int index, Color color) {
            if (index % 2 == 0) {
                // left eye
                index /= 2;
                LeftLEDs.SetColor(index, color);
            } else {
                // right eye
                index = (index - 1) / 2;
                RightLEDs.SetColor(index, color);
            }
        }

        public async Task StartPickerCaptureAsync() {
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(_hwnd);
            var item = await picker.PickSingleItemAsync();

            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
                LedActive = true;
            }
        }

        public void StartHwndCapture(IntPtr hwnd) {
            var item = CaptureHelper.CreateItemForWindow(hwnd);
            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
                LedActive = true;
            }
        }

        public void StartHmonCapture(IntPtr hmon) {
            var item = CaptureHelper.CreateItemForMonitor(hmon);
            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
                LedActive = true;
            }
        }

        public void StopCapture() {
            _captureApp.StopCapture();
            LedActive = false;
        }

        private void SetLedCount(int count) {
            _captureApp.NumberOfLedsPerEye = count;
            LeftLEDs.Amount = count;
            RightLEDs.Amount = count;
        }

        private void SetLedBlur(float blurPercentage) {
            var ledSize = LeftLEDs.Size;

            var maxBlurSize = ledSize * 2f;
            // TODO: make depending on amount of leds too

            LeftLEDs.BlurRadius = maxBlurSize * blurPercentage;
            RightLEDs.BlurRadius = maxBlurSize * blurPercentage;
        }

        private void SetLedSize(float size) {
            LeftLEDs.Size = size;
            RightLEDs.Size = size;
        }

        private void SetLedActive(bool isActive) {
            LeftLEDs.IsActive = isActive;
            RightLEDs.IsActive = isActive;
        }

        private void SetLedShowColorvalue(bool showColorvalue) {
            LeftLEDs.ShowColorValue = showColorvalue;
            RightLEDs.ShowColorValue = showColorvalue;
        }

        private void SetSampleAreas(bool showSampleAreas) {
            // TODO: implement
        }
    }
}