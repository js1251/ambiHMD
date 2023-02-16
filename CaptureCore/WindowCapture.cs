using System;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace CaptureCore {
    public sealed class WindowCapture : IDisposable {
        public SharpDX.Direct3D11.Device D3dDevice { get; }
        public event EventHandler<Texture2D> TextureChanged;
        public event EventHandler<(int, int)> TextureSizeChanged;

        private readonly GraphicsCaptureItem _item;
        private readonly Direct3D11CaptureFramePool _framePool;
        private readonly GraphicsCaptureSession _session;
        private SizeInt32 _lastSize;

        private readonly IDirect3DDevice _device;
        private readonly SwapChain1 _swapChain;

        public WindowCapture(IDirect3DDevice d, GraphicsCaptureItem i) {
            _item = i;
            _device = d;
            D3dDevice = Direct3D11Helper.CreateSharpDXDevice(_device);

            var dxgiFactory = new Factory2();
            var description = new SwapChainDescription1 {
                Width = _item.Size.Width,
                Height = _item.Size.Height,
                Format = Format.B8G8R8A8_UNorm,
                Stereo = false,
                SampleDescription = new SampleDescription {
                    Count = 1,
                    Quality = 0
                },
                Usage = Usage.RenderTargetOutput,
                BufferCount = 2,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.FlipSequential,
                AlphaMode = AlphaMode.Premultiplied,
                Flags = SwapChainFlags.None
            };

            _swapChain = new SwapChain1(dxgiFactory, D3dDevice, ref description);

            _framePool =
                Direct3D11CaptureFramePool.Create(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, i.Size);
            _session = _framePool.CreateCaptureSession(i);
            //_lastSize = i.Size;

            _framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose() {
            _session?.Dispose();
            _framePool?.Dispose();
            _swapChain?.Dispose();
            D3dDevice?.Dispose();
        }

        public void StartCapture() {
            _session.StartCapture();
        }

        public ICompositionSurface CreateSurface(Compositor compositor) {
            return compositor.CreateCompositionSurfaceForSwapChain(_swapChain);
        }

        private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args) {
            var newSize = false;

            using (var frame = sender.TryGetNextFrame()) {
                if (frame.ContentSize.Width != _lastSize.Width || frame.ContentSize.Height != _lastSize.Height) {
                    // The thing we have been capturing has changed size.
                    // We need to resize the swap chain first, then blit the pixels.
                    // After we do that, retire the frame and then recreate the frame pool.
                    newSize = true;
                    _lastSize = frame.ContentSize;
                    TextureSizeChanged?.Invoke(this, (_lastSize.Width, _lastSize.Height));
                    
                    _swapChain.ResizeBuffers(2,
                        _lastSize.Width,
                        _lastSize.Height,
                        Format.B8G8R8A8_UNorm,
                        SwapChainFlags.None);
                }

                using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface)) {
                    D3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);
                    TextureChanged?.Invoke(this, bitmap);
                }
            }

            _swapChain.Present(0, PresentFlags.None);

            if (newSize) {
                _framePool.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _lastSize);
            }
        }
    }
}