using System;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using ambiHMD.Communication;
using Windows.UI;

namespace CaptureCore {
    public sealed class CaptureApplication : IDisposable {
        #region Properties

        public delegate void ColorChangedArg(CaptureApplication sender, int index, byte[] colorData);

        public event ColorChangedArg ColorChanged;
        public Visual Visual => _root;

        public int NumberOfLedsPerEye {
            private get => _numberOfLedPerEye;
            set {
                _numberOfLedPerEye = value;
                _encoder.NumLeds = value * FrameProcessor.NUMBER_OF_EYES;
                if (_frameProcessor != null) {
                    _frameProcessor.NumberOfLedsPerEye = value;

                    CreateSweepAreas();
                    ResizeSweepAreas();
                }
            }
        }

        public float VerticalSweep {
            set {
                _verticalSweep = value;

                if (_frameProcessor != null) {
                    _frameProcessor.VerticalSweep = value;
                    ResizeSweepAreas();
                }
            }
        }

        public float HorizontalSweep {
            set {
                _horizontalSweep = value;

                if (_frameProcessor != null) {
                    _frameProcessor.HorizontalSweep = value;
                    ResizeSweepAreas();
                }
            }
        }

        public bool ShowSampleAreas {
            get => _showSampleAreas;
            set {
                if (!value) {
                    _rectVisual.Shapes.Clear();
                    return;
                }

                _showSampleAreas = value;
                if (_frameProcessor != null) {
                    CreateSweepAreas();
                    ResizeSweepAreas();
                }
            }
        }

        public float GammaCorrection {
            set => _encoder.GammaCorrection = value;
        }

        public float LuminanceCorrection {
            set => _encoder.LuminanceCorrection = value;
        }

        public float Smoothing {
            set => _encoder.Smoothing = value;
        }

        private float _horizontalSweep;
        private float _verticalSweep;

        public int Brightness {
            set => _encoder.Brightness = value;
        }

        public int ComPort {
            set => _ambiHmdConnection = new AmbiHMDConnection(value, 115200);
        }

        public double CaptureWidth { get; set; }
        public double CaptureHeight { get; set; }
        public double WindowHeight { get; set; }

        #endregion Properties

        #region Fields

        private Compositor _compositor;
        private readonly ContainerVisual _root;

        private readonly SpriteVisual _content;
        public CompositionSurfaceBrush Brush { get; private set; }

        private readonly IDirect3DDevice _device;
        private WindowCapture _capture;

        private FrameProcessor _frameProcessor;
        private AmbiHMDEncoder _encoder;
        private int _numberOfLedPerEye;

        private AmbiHMDConnection _ambiHmdConnection;
        private ShapeVisual _rectVisual;
        private bool _showSampleAreas;

        #endregion Fields

        public CaptureApplication(Compositor c) {
            _compositor = c;
            _device = Direct3D11Helper.CreateDevice();

            // Setup the root.
            _root = _compositor.CreateContainerVisual();
            _root.RelativeSizeAdjustment = Vector2.One;

            // Setup the content.
            Brush = _compositor.CreateSurfaceBrush();
            Brush.HorizontalAlignmentRatio = 0.5f;
            Brush.VerticalAlignmentRatio = 0.5f;
            Brush.Stretch = CompositionStretch.Uniform;

            var shadow = _compositor.CreateDropShadow();
            shadow.Mask = Brush;

            _content = _compositor.CreateSpriteVisual();
            _content.AnchorPoint = new Vector2(0.5f, 0.5f);
            _content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            _content.RelativeSizeAdjustment = Vector2.One;
            _content.Brush = Brush;
            _content.Shadow = shadow;
            _root.Children.InsertAtTop(_content);

            _rectVisual = _compositor.CreateShapeVisual();
            _rectVisual.AnchorPoint = new Vector2(0.5f, 0.5f);
            _rectVisual.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            _rectVisual.RelativeSizeAdjustment = Vector2.One;
            _root.Children.InsertAtTop(_rectVisual);

            _encoder = new AmbiHMDEncoder(NumberOfLedsPerEye * FrameProcessor.NUMBER_OF_EYES,
                FrameProcessor.DATA_STRIDE);
        }

        private void CreateSweepAreas() {
            if (!ShowSampleAreas) {
                return;
            }

            _rectVisual.Shapes.Clear();

            var strokeBrush = _compositor.CreateColorBrush(Colors.DarkRed);
            for (var x = 0; x < FrameProcessor.NUMBER_OF_EYES; x++) {
                for (var y = 0; y < _numberOfLedPerEye; y++) {
                    var rect = _compositor.CreateRectangleGeometry();
                    var rectShape = _compositor.CreateSpriteShape(rect);
                    rectShape.StrokeBrush = strokeBrush;
                    _rectVisual.Shapes.Add(rectShape);
                }
            }
        }

        public void ResizeSweepAreas() {
            if (!ShowSampleAreas) {
                return;
            }

            for (var i = 0; i < _rectVisual.Shapes.Count; i++) {
                var xIndex = i >= _numberOfLedPerEye ? 1 : 0;
                var yIndex = xIndex == 1 ? i - _numberOfLedPerEye : i;

                var (left, top, right, bottom) = _frameProcessor.GetStencil(xIndex, yIndex, (int)CaptureWidth, (int)CaptureHeight);

                var offset = (int)(WindowHeight / 2f - CaptureHeight / 2f);

                top += offset;
                bottom += offset;

                var rect = (CompositionRectangleGeometry)((CompositionSpriteShape)_rectVisual.Shapes[i]).Geometry;
                rect.Size = new Vector2(right - left, bottom - top);
                rect.Offset = new Vector2(left, top);
            }
        }

        public void Dispose() {
            StopCapture();
            _compositor = null;
            _root.Dispose();
            _content.Dispose();
            Brush.Dispose();
            _device.Dispose();
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item) {
            StopCapture();
            _capture = new WindowCapture(_device, item);

            var surface = _capture.CreateSurface(_compositor);
            Brush.Surface = surface;

            _capture.StartCapture();

            _frameProcessor =
                new FrameProcessor(_capture.D3dDevice, NumberOfLedsPerEye, _verticalSweep, _horizontalSweep);

            CreateSweepAreas();

            _capture.TextureChanged += UpdateLedValues;
            _capture.TextureSizeChanged += UpdateFrameSize;
        }

        public void StopCapture() {
            _capture?.Dispose();
            Brush.Surface = null;
            _frameProcessor = null;

            // TODO: turn all LEDs off
            //_ambiHmdConnection?.SendMessage(AmbiHMDEncoder.NullMessage());
        }

        private void UpdateLedValues(object sender, Texture2D texture) {
            var ledData = _frameProcessor.ProcessFrame(texture);

            var encoded = _encoder.Encode(ledData.Data);
            _ambiHmdConnection?.SendMessage(encoded);

            var ledAmount = NumberOfLedsPerEye * FrameProcessor.NUMBER_OF_EYES;

            for (var i = 0; i < ledAmount; i++) {
                var colorData = ledData.GetData(i);
                ColorChanged?.Invoke(this, i, colorData);
            }
        }

        private void UpdateFrameSize(object sender, (int, int) size) {
            _frameProcessor.SetFrameSize(size.Item1, size.Item2);
        }
    }
}