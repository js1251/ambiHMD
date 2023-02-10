using System.Runtime.InteropServices;
using System;
using SharpDX.Direct3D11;

namespace FrameProcessing {
    public sealed class FrameProcessor {
        public const int NUMBER_OF_EYES = 2;
        public const int SIZE_OF_DATAPOINT = 4;

        public int NumberOfLedsPerEye {
            get => _numberOfLedsPerEye;
            set {
                _numberOfLedsPerEye = value;
                InitializeStagingBuffer();
            }
        }

        private readonly Device _d3dDevice;
        private ComputeShader _computeShader;
        private SharpDX.Direct3D11.Buffer _stagingBuffer;
        private int _numberOfLedsPerEye;

        public FrameProcessor(LedComputeShader computeShader, Device d3dDevice, int numberOfLedsPerEye) {
            _computeShader = computeShader.GetComputeShader(d3dDevice);
            _d3dDevice = d3dDevice;
            _numberOfLedsPerEye = numberOfLedsPerEye;
            
            InitializeStagingBuffer();
        }

        public byte[] FrocessFrame(Texture2D frame) {
            // set the shader
            _d3dDevice.ImmediateContext.ComputeShader.Set(_computeShader);

            var view = new UnorderedAccessView(_d3dDevice,
                frame,
                new UnorderedAccessViewDescription {
                    Format = SharpDX.DXGI.Format.R8G8B8A8_UNorm,
                    Dimension = UnorderedAccessViewDimension.Texture2D,
                    Texture2D = {
                        MipSlice = 0
                    }
                });

            // give it access to the view
            _d3dDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, view);

            // send it off to run
            _d3dDevice.ImmediateContext.Dispatch(32, 32, 1);

            // copy the results into staging resource
            _d3dDevice.ImmediateContext.CopyResource(frame, _stagingBuffer);

            // get access to the staging resource on CPU
            var mapSource = _d3dDevice.ImmediateContext.MapSubresource(_stagingBuffer, 0, MapMode.Read, MapFlags.None);

            // TODO: parse staging resource into led value array
            Console.WriteLine(Marshal.ReadInt32(IntPtr.Add(mapSource.DataPointer, 0)));

            throw new NotImplementedException();
        }

        private void InitializeStagingBuffer() {
            // staging buffer
            _stagingBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                new BufferDescription {
                    CpuAccessFlags = CpuAccessFlags.Read,
                    BindFlags = BindFlags.None,
                    SizeInBytes = NumberOfLedsPerEye * NUMBER_OF_EYES * SIZE_OF_DATAPOINT,
                    OptionFlags = ResourceOptionFlags.None,
                    Usage = ResourceUsage.Staging
                });
        }
    }
}