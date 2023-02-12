using System;
using System.IO;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;

namespace CaptureCore {
    public sealed class CaptureApplication : IDisposable {

        public event EventHandler<(byte, byte, byte, byte)> ColorChanged;
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
        private readonly CompositionSurfaceBrush _brush;

        private readonly IDirect3DDevice _device;
        private WindowCapture _capture;
        
        private readonly ComputeShaderCompileHelper _ledShader;
        private FrameProcessor _frameProcessor;
        private int _numberOfLedPerEye;

        public CaptureApplication(Compositor c) {
            _compositor = c;
            _device = Direct3D11Helper.CreateDevice();

            // Setup the root.
            _root = _compositor.CreateContainerVisual();
            _root.RelativeSizeAdjustment = Vector2.One;

            // Setup the content.
            _brush = _compositor.CreateSurfaceBrush();
            _brush.HorizontalAlignmentRatio = 0.5f;
            _brush.VerticalAlignmentRatio = 0.5f;
            _brush.Stretch = CompositionStretch.Uniform;

            var shadow = _compositor.CreateDropShadow();
            shadow.Mask = _brush;

            _content = _compositor.CreateSpriteVisual();
            _content.AnchorPoint = new Vector2(0.5f, 0.5f);
            _content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            _content.RelativeSizeAdjustment = Vector2.One;
            _content.Size = new Vector2(-80, -80);
            _content.Brush = _brush;
            _content.Shadow = shadow;
            _root.Children.InsertAtTop(_content);

            _ledShader = new ComputeShaderCompileHelper(new FileInfo("example.hlsl"));
        }

        public Visual Visual => _root;

        public void Dispose() {
            StopCapture();
            _compositor = null;
            _root.Dispose();
            _content.Dispose();
            _brush.Dispose();
            _device.Dispose();
        }

        public void StartCaptureFromItem(GraphicsCaptureItem item) {
            StopCapture();
            _capture = new WindowCapture(_device, item);

            var surface = _capture.CreateSurface(_compositor);
            _brush.Surface = surface;

            _capture.StartCapture();

            _frameProcessor = new FrameProcessor(_ledShader, _capture.D3dDevice, NumberOfLedsPerEye);

            _capture.TextureChanged += UpdateLedValues;
        }

        public void StopCapture() {
            _capture?.Dispose();
            _brush.Surface = null;
            _frameProcessor = null;
        }

        private void UpdateLedValues(object sender, Texture2D texture) {
            var ledData = _frameProcessor.ProcessFrame(texture);
            var ledAmount = NumberOfLedsPerEye * FrameProcessor.NUMBER_OF_EYES;

            for (var i = 0; i < ledAmount; i++) {
                var colorData = ledData.GetColor(i);
                ColorChanged?.Invoke(this, colorData);
            }
        }
    }
}