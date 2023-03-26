using System;
using System.Runtime.InteropServices;
using Windows.UI.Xaml.Media;
using SharpDX.Direct3D11;
using WaveEngine.Bindings.RenderDoc;

namespace CaptureCore {
    public sealed class FrameProcessor {
        public const int NUMBER_OF_EYES = 2;
        public const int DATA_STRIDE = 4; //RGBA -> 4 bytes each

        public int NumberOfLedsPerEye {
            get => _numberOfLedsPerEye;
            set {
                _numberOfLedsPerEye = value;
                InitializeBuffers();
            }
        }

        // TODO: set externally
        public float VerticalSweep {
            get => _verticalSweep;
            set {
                _verticalSweep = value;
                InitializeBuffers();
            }
        }

        // TODO: set externally
        public float HorizontalSweep {
            get => _horizontalSweep;
            set {
                _horizontalSweep = value;
                InitializeBuffers();
            }
        }

        #region Backing Fields

        private int _numberOfLedsPerEye;
        private int _frameWidth;
        private int _frameHeight;
        private float _verticalSweep;
        private float _horizontalSweep;

        #endregion Backing Fields

        private readonly Device _d3dDevice;

        private Resource _workBuffer;
        private Resource _stagingBuffer;
        private ShaderResourceView _srv;

        //private readonly RenderDoc _renderDoc;

        public FrameProcessor(Device d3dDevice, int numberOfLedsPerEye, float verticalSweep, float horizontalSweep) {
            _d3dDevice = d3dDevice;
            _numberOfLedsPerEye = numberOfLedsPerEye;
            _verticalSweep = verticalSweep;
            _horizontalSweep = horizontalSweep;

            //RenderDoc.Load(out _renderDoc);
        }

        public void SetFrameSize(int width, int height) {
            _frameWidth = width;
            _frameHeight = height;

            InitializeBuffers();
        }

        public (int, int, int, int) GetStencil(int xIndex, int yIndex, int targetWidth, int targetHeight) {
            var xRightStart = targetWidth - HorizontalSweep * targetWidth;
            var left = (int)Math.Floor(xIndex > 0 ? xRightStart : 0);

            var yDivider = (float)targetHeight / NumberOfLedsPerEye;
            var yMiddleOffset = yIndex * yDivider + 0.5f * yDivider;
            var yTopOffset = yMiddleOffset - 0.5f * targetHeight * VerticalSweep;
            var top = (int)Math.Floor(yTopOffset);
            top = Math.Max(top, 0);

            var right = left + (int)Math.Floor(HorizontalSweep * targetWidth);
            var bottom = top + (int)Math.Floor(VerticalSweep * targetHeight);
            bottom = Math.Min(bottom, targetHeight);

            return (left, Math.Max(top, 0), right, bottom);
        }

        public LedData ProcessFrame(Texture2D frame) {
            var ledData = new LedData(NumberOfLedsPerEye * NUMBER_OF_EYES, DATA_STRIDE);

            for (var y = 0; y < NumberOfLedsPerEye; y++) {
                for (var x = 0; x < NUMBER_OF_EYES; x++) {
                    var (left, top, right, bottom) = GetStencil(x, y, _frameWidth, _frameHeight);

                    var sourceRegion = new ResourceRegion {
                        Left = left,
                        Right = right,
                        Top = top,
                        Bottom = bottom,
                        Front = 0,
                        Back = 1
                    };

                    //_renderDoc?.API.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);

                    // copy part of frame into workBuffer
                    _d3dDevice.ImmediateContext.CopySubresourceRegion(frame, 0, sourceRegion, _workBuffer, 0);

                    // create mipmap for workBuffer
                    _d3dDevice.ImmediateContext.GenerateMips(_srv);

                    // find out the biggest mip level from the workBuffer
                    var maxMipLevels = _srv.Description.Texture2D.MipLevels;

                    // get the index to the smallest mip
                    // visual reference: https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-resources-subresources
                    var subresourceIndex = Resource.CalculateSubResourceIndex(maxMipLevels - 1, 0, maxMipLevels);

                    // copy smallest mip of workBuffer into stagingBuffer
                    _d3dDevice.ImmediateContext.CopySubresourceRegion(_workBuffer, subresourceIndex, null, _stagingBuffer, 0);

                    // get access to the staging buffer on CPU
                    var mapSource = _d3dDevice.ImmediateContext.MapSubresource(_stagingBuffer, 0, MapMode.Read, MapFlags.None);

                    // copy the data into byte array
                    var data = new byte[DATA_STRIDE];
                    Marshal.Copy(mapSource.DataPointer, data, 0, data.Length);

                    // un-map the staging buffer
                    _d3dDevice.ImmediateContext.UnmapSubresource(_stagingBuffer, 0);

                    //_renderDoc?.API.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);

                    // TODO: how big is data ? What format is it?
                    ledData.SetData(y * NUMBER_OF_EYES + x, data);
                }
            }

            return ledData;
        }

        private void InitializeBuffers() {
            var stencilWidth = (int)Math.Floor(HorizontalSweep * _frameWidth);
            var stencilHeight = (int)Math.Floor(VerticalSweep * _frameHeight);

            _workBuffer = new Texture2D(_d3dDevice,
                new Texture2DDescription {
                    Width = stencilWidth,
                    Height = stencilHeight,
                    MipLevels = 0, // 0 = full mipmap down to 1x1 pixel texture
                    ArraySize = 1, // only one texture used
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, // same as frame
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, // required by GenerateMips
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.GenerateMipMaps // required by GenerateMips
                });

            _srv = new ShaderResourceView(_d3dDevice, _workBuffer);

            // staging buffer that will hold the smallest mip of the workbuffer
            _stagingBuffer = new Texture2D(_d3dDevice,
                new Texture2DDescription {
                    Width = 1,
                    Height = 1,
                    MipLevels = 0,
                    ArraySize = 1,
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm,
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    CpuAccessFlags = CpuAccessFlags.Read,
                    OptionFlags = ResourceOptionFlags.None
                });
        }
    }
}