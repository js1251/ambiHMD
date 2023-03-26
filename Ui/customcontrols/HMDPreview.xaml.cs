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
        #region Properties

        public int LedPerEye {
            get => _ledPerEye;
            set {
                _ledPerEye = value;
                _captureApp.NumberOfLedsPerEye = value;
                LeftLEDs.Amount = value;
                RightLEDs.Amount = value;
            }
        }

        public float BlurPercentage {
            set {
                var ledSize = LeftLEDs.Size;
                var maxBlurSize = ledSize * 1.25f;

                LeftLEDs.BlurRadius = maxBlurSize * value;
                RightLEDs.BlurRadius = maxBlurSize * value;
            }
        }

        public int Brightness {
            set => _captureApp.Brightness = value;
        }

        public float VerticalSweep {
            set => _captureApp.VerticalSweep = value;
        }

        public float HorizontalSweep {
            set => _captureApp.HorizontalSweep = value;
        }

        public float LedSize {
            set {
                LeftLEDs.Size = value;
                RightLEDs.Size = value;
            }
        }

        public bool LedActive {
            set {
                LeftLEDs.IsActive = value;
                RightLEDs.IsActive = value;
            }
        }

        public bool ShowColorValue {
            set {
                LeftLEDs.ShowColorValue = value;
                RightLEDs.ShowColorValue = value;
            }
        }

        public bool ShowSampleAreas {
            set => _captureApp.ShowSampleAreas = value;
        }

        public float GammaCorrection {
            set => _captureApp.GammaCorrection = value;
        }

        public float LuminanceCorrection {
            set => _captureApp.LuminanceCorrection = value;
        }

        public float Smoothing {
            set => _captureApp.Smoothing = value;
        }

        public int ComPort {
            set => _captureApp.ComPort = value;
        }

        #endregion Properties

        #region Fields

        private int _ledPerEye;
        private IntPtr _hwnd;
        private Compositor _compositor;
        private CompositionTarget _target;
        private ContainerVisual _root;
        private CaptureApplication _captureApp;
        private Window _window;
        private GraphicsCaptureItem _currentItem;

        #endregion Fields

        public HMDPreview() {
            InitializeComponent();
        }

        /// <summary>
        /// Resizes the preview window to the full screen leftOffset minus the given leftOffset to the left.
        /// Used for when the settings tab on the left is expanded or collapsed.
        /// </summary>
        /// <param name="leftOffset">The amount of leftOffset to the left of the preview</param>
        public void Resize(double leftOffset) {
            var presentationSource = PresentationSource.FromVisual(this);
            var dpiX = presentationSource.CompositionTarget.TransformToDevice.M11;
            // var dpiY = presentationSource.CompositionTarget.TransformToDevice.M22;

            var controlsWidth = (float)(leftOffset * dpiX);

            var padding = LeftLEDs.Size + RightLEDs.Size; // led
            padding += (float)(LeftLEDs.Margin.Left + LeftLEDs.Margin.Right);
            padding += (float)(RightLEDs.Margin.Left + RightLEDs.Margin.Right);
            padding *= (float)dpiX;

            _root.Size = new Vector2(-(controlsWidth + padding), 0);
            _root.Offset = new Vector3(controlsWidth + padding * 0.5f, 0, 0);

            ResizeLedHeight();
            _captureApp.WindowHeight = ActualHeight;
            _captureApp.ResizeSweepAreas();
        }

        private void ResizeLedHeight() {
            if (_currentItem is null) {
                return;
            }

            var dpiX = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice.M11;
            var previewWidth = _window.Width + _root.Size.X * 1 / dpiX;
            var aspectRatio = _currentItem.Size.Width / (float)_currentItem.Size.Height;

            var previewHeight = previewWidth * 1 / aspectRatio;
            LeftLEDs.Height = previewHeight;
            RightLEDs.Height = previewHeight;

            _captureApp.CaptureWidth = previewWidth;
            _captureApp.CaptureHeight = previewHeight;
        }

        private void ResetLedHeight() {
            LeftLEDs.Height = double.NaN;
            RightLEDs.Height = double.NaN;
        }

        public void Window_Loaded(object sender, RoutedEventArgs e) {
            _window = sender as Window;
            var interopWindow = new WindowInteropHelper(_window);
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
            _root.Children.InsertAtTop(_captureApp.Visual);

            _window.SizeChanged += (object _, SizeChangedEventArgs args) => {
                ResizeLedHeight();

                _captureApp.WindowHeight = args.NewSize.Height - SystemParameters.WindowCaptionHeight;
                _captureApp.ResizeSweepAreas();
            };
            
            // detect fullscreen toggle
            _window.StateChanged += (object _, EventArgs __) => {
                ResizeLedHeight();
                _captureApp.WindowHeight = ActualHeight;
                _captureApp.ResizeSweepAreas();
            };

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
            }
            else {
                // right eye
                index = (index - 1) / 2;
                RightLEDs.SetColor(index, color);
            }
        }

        public async Task<GraphicsCaptureItem> StartPickerCaptureAsync() {
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(_hwnd);
            var item = await picker.PickSingleItemAsync();

            if (item != null) {
                _captureApp.StartCaptureFromItem(item);
                LedActive = true;
            }

            return item;
        }

        public void StartHwndCapture(IntPtr hwnd) {
            _currentItem = CaptureHelper.CreateItemForWindow(hwnd);
            if (_currentItem != null) {
                _captureApp.StartCaptureFromItem(_currentItem);
                ResizeLedHeight();
                LedActive = true;
            }
        }

        public void StartHmonCapture(IntPtr hmon) {
            _currentItem = CaptureHelper.CreateItemForMonitor(hmon);
            if (_currentItem != null) {
                _captureApp.StartCaptureFromItem(_currentItem);
                ResizeLedHeight();
                LedActive = true;
            }
        }

        public void StopCapture() {
            _captureApp.StopCapture();
            _currentItem = null;
            LedActive = false;
            ResetLedHeight();
        }
    }
}