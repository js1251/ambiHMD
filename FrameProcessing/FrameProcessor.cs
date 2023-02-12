using System.Runtime.InteropServices;
using SharpDX.Direct3D11;

namespace FrameProcessing {
    public sealed class FrameProcessor {
        public const int NUMBER_OF_EYES = 2;
        public const int DATA_STRIDE = 32; //RGBA

        public int NumberOfLedsPerEye {
            get => _numberOfLedsPerEye;
            set {
                _numberOfLedsPerEye = value;
                InitializeBuffers();
            }
        }

        private readonly Device _d3dDevice;
        private readonly ComputeShader _computeShader;
        private Buffer _workBuffer;
        private UnorderedAccessView _workBufferUav;
        private Buffer _stagingBuffer;
        private int _numberOfLedsPerEye;

        public FrameProcessor(LedComputeShader ledComputeShader, Device d3dDevice, int numberOfLedsPerEye) {
            _computeShader = ledComputeShader.GetComputeShader(d3dDevice);
            _d3dDevice = d3dDevice;
            _numberOfLedsPerEye = numberOfLedsPerEye;

            InitializeBuffers();
        }

        public LedData ProcessFrame(Texture2D frame) {
            // set the shader
            _d3dDevice.ImmediateContext.ComputeShader.Set(_computeShader);

            var srv = new ShaderResourceView(_d3dDevice, frame);

            _d3dDevice.ImmediateContext.ComputeShader.SetShaderResource(0, srv);
            _d3dDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, _workBufferUav);

            // send it off to run
            _d3dDevice.ImmediateContext.Dispatch(32, 32, 1);

            // copy the results into staging resource
            _d3dDevice.ImmediateContext.CopyResource(_workBuffer, _stagingBuffer);

            // get access to the staging resource on CPU
            var mapSource = _d3dDevice.ImmediateContext.MapSubresource(_stagingBuffer, 0, MapMode.Read, MapFlags.None);

            // copy the data into a managed array
            var data = new byte[mapSource.RowPitch];
            Marshal.Copy(mapSource.DataPointer, data, 0, data.Length);

            // unmap the staging resource
            _d3dDevice.ImmediateContext.UnmapSubresource(_stagingBuffer, 0);

            return new LedData(data, NumberOfLedsPerEye * NUMBER_OF_EYES, DATA_STRIDE);
        }

        private void InitializeBuffers() {
            _workBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                new BufferDescription(
                    GetPaddedSize(NumberOfLedsPerEye * NUMBER_OF_EYES * DATA_STRIDE, 16),
                    ResourceUsage.Default,
                    BindFlags.ShaderResource | BindFlags.UnorderedAccess,
                    CpuAccessFlags.None,
                    ResourceOptionFlags.BufferStructured,
                    DATA_STRIDE
                )
            );

            _workBufferUav = new UnorderedAccessView(_d3dDevice, _workBuffer);

            _stagingBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                new BufferDescription {
                    SizeInBytes = GetPaddedSize(NumberOfLedsPerEye * NUMBER_OF_EYES * DATA_STRIDE, 16),
                    CpuAccessFlags = CpuAccessFlags.Read,
                    Usage = ResourceUsage.Staging,
                    BindFlags = BindFlags.None,
                    OptionFlags = ResourceOptionFlags.None,
                    StructureByteStride = DATA_STRIDE
                });
        }

        private static int GetPaddedSize(int unpadded, int divisor) {
            // pad to nearest 16 divisible
            var remainder = unpadded % divisor;
            if (remainder != 0) {
                unpadded += divisor - remainder;
            }

            return unpadded;
        }
    }
}