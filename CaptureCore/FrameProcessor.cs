using System;
using System.Runtime.InteropServices;
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
        public float SampleWidth {
            get => _sampleWidth;
            set {
                _sampleWidth = value;
                InitializeBuffers();
            }
        }

        // TODO: set externally
        public float SampleHeight {
            get => _sampleHeight;
            set {
                _sampleHeight = value;
                InitializeBuffers();
            }
        }

        #region Backing Fields

        private int _numberOfLedsPerEye;
        private int _frameWidth;
        private int _frameHeight;
        private float _sampleWidth;
        private float _sampleHeight;

        #endregion Backing Fields

        private readonly Device _d3dDevice;
        
        private Resource _workBuffer;
        private Resource _stagingBuffer;

        private float _xRightStart;
        private float _yStep;
        private int _maxWorkMipLevel;

        private readonly RenderDoc _renderDoc;

        public FrameProcessor(Device d3dDevice, int numberOfLedsPerEye) {
            _d3dDevice = d3dDevice;
            _numberOfLedsPerEye = numberOfLedsPerEye;

            RenderDoc.Load(out _renderDoc);
        }

        public void SetFrameSize(int width, int height) {
            _frameWidth = width;
            _frameHeight = height;

            InitializeBuffers();
        }

        public LedData ProcessFrame(Texture2D frame) {
            var ledData = new LedData(NumberOfLedsPerEye * NUMBER_OF_EYES, DATA_STRIDE);

            for (var y = 0; y < NumberOfLedsPerEye; y++) {
                for (var x = 0; x < NUMBER_OF_EYES; x++) {
                    var xStart = (int)Math.Floor(x > 0 ? _xRightStart : 0);
                    var yStart = (int)Math.Floor(y * _yStep);

                    _renderDoc?.API.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);

                    var stencilWidth = (int)Math.Floor(SampleWidth * _frameWidth);
                    var stencilHeight = (int)Math.Floor(SampleHeight * _frameHeight);
                    var sourceRegion = new ResourceRegion {
                        Left = xStart,
                        Top = yStart,
                        Front = 0,
                        Right = xStart + stencilWidth,
                        Bottom = yStart + stencilHeight,
                        Back = 1
                    };

                    // copy part of frame into workBuffer
                    _d3dDevice.ImmediateContext.CopySubresourceRegion(frame, 0, sourceRegion, _workBuffer, 0);

                    // create mipmap for workBuffer
                    var srv = new ShaderResourceView(_d3dDevice, _workBuffer);
                    _d3dDevice.ImmediateContext.GenerateMips(srv);

                    // get the index to the smallest mip
                    // visual reference: https://learn.microsoft.com/en-us/windows/win32/direct3d11/overviews-direct3d-11-resources-subresources
                    var subresourceIndex = Resource.CalculateSubResourceIndex(_maxWorkMipLevel - 1, 0, _maxWorkMipLevel);

                    // copy smallest mip of workBuffer into stagingBuffer
                    _d3dDevice.ImmediateContext.CopySubresourceRegion(_workBuffer, subresourceIndex, null, _stagingBuffer, 0);
                    
                    // get access to the staging buffer on CPU
                    var mapSource = _d3dDevice.ImmediateContext.MapSubresource(_stagingBuffer, 0, MapMode.Read, MapFlags.None);

                    // copy the data into byte array
                    var data = new byte[mapSource.RowPitch]; // TODO: what does RowPitch mean?
                    Marshal.Copy(mapSource.DataPointer, data, 0, data.Length);

                    // un-map the staging buffer
                    _d3dDevice.ImmediateContext.UnmapSubresource(_stagingBuffer, 0);

                    // release resources
                    srv.Dispose();

                    _renderDoc?.API.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);

                    // TODO: how big is data ? What format is it?
                    ledData.SetData(y * NUMBER_OF_EYES + x, data);
                }
            }

            return ledData;
        }

        private void InitializeBuffers() {
            // TODO: temporarily set SampleWidth and Height to hardcoded values
            _sampleWidth = 0.1f; // 10% of the frame width
            _sampleHeight = 1f / NumberOfLedsPerEye; // 1/NumberOfLedsPerEye of the frame height
            // TODO: this will be more complicated once stencils can be bigger, since they could reach out of the frame

            _yStep = _frameHeight * _sampleHeight;
            _xRightStart = _frameWidth - SampleWidth * _frameWidth;

            var stencilWidth = (int)Math.Floor(SampleWidth * _frameWidth);
            var stencilHeight = (int)Math.Floor(SampleHeight * _frameHeight);

            _maxWorkMipLevel = (int)Math.Ceiling(Math.Log(Math.Max(stencilWidth, stencilHeight), 2)) + 1;

            // Buffer that will hold a sub-region of the full frame
            _workBuffer = new Texture2D(_d3dDevice,
                new Texture2DDescription {
                    Width = (int)(SampleWidth * _frameWidth),
                    Height = (int)(SampleHeight * _frameHeight),
                    MipLevels = _maxWorkMipLevel, // amount of mip levels required to produce a 1x1 pixel texture
                    ArraySize = 1, // only one texture used
                    Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, // same as frame
                    SampleDescription = new SharpDX.DXGI.SampleDescription(1, 0),
                    Usage = ResourceUsage.Default,
                    BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource, // required by GenerateMips
                    CpuAccessFlags = CpuAccessFlags.None,
                    OptionFlags = ResourceOptionFlags.GenerateMipMaps // required by GenerateMips
                });

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