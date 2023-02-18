using System;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;

namespace CaptureCore {
    public sealed class CaptureApplication : IDisposable {

        public delegate void ColorChangedArg(CaptureApplication sender, int index, byte[] colorData);

        public event ColorChangedArg ColorChanged;
        public int NumberOfLedsPerEye {
            private get => _numberOfLedPerEye;
            set {
                _numberOfLedPerEye = value;
                if (_frameProcessor != null) {
                    _frameProcessor.NumberOfLedsPerEye = value;
                }
            }
        }

        private Compositor _compositor;
        private readonly ContainerVisual _root;

        private readonly SpriteVisual _content;
        public CompositionSurfaceBrush Brush { get; private set; }

        private readonly IDirect3DDevice _device;
        private WindowCapture _capture;
        
        private FrameProcessor _frameProcessor;
        private int _numberOfLedPerEye;

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
            //_content.Size = new Vector2(-80, -80);
            _content.Brush = Brush;
            _content.Shadow = shadow;
            _root.Children.InsertAtTop(_content);
        }

        public Visual Visual => _root;

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

            _frameProcessor = new FrameProcessor(_capture.D3dDevice, NumberOfLedsPerEye);

            _capture.TextureChanged += UpdateLedValues;
            _capture.TextureSizeChanged += UpdateFrameSize;
        }

        public void StopCapture() {
            _capture?.Dispose();
            Brush.Surface = null;
            _frameProcessor = null;
        }

        private void UpdateLedValues(object sender, Texture2D texture) {
            var ledData = _frameProcessor.ProcessFrame(texture);
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