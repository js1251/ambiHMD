using System.Runtime.InteropServices;
using System;
using SharpDX;
using SharpDX.Direct3D11;
using WaveEngine.Bindings.RenderDoc;

namespace FrameProcessing {
    public sealed class FrameProcessor {
        public const int NUMBER_OF_EYES = 2;
        public const int SIZE_OF_DATAPOINT = 4;

        public int NumberOfLedsPerEye {
            get => _numberOfLedsPerEye;
            set {
                _numberOfLedsPerEye = value;
                InitializeBuffers();
            }
        }

        private readonly Device _d3dDevice;
        private ComputeShader _computeShader;
        private SharpDX.Direct3D11.Buffer _workBuffer;
        private UnorderedAccessView _workBufferUav;
        private SharpDX.Direct3D11.Buffer _stagingBuffer;
        private int _numberOfLedsPerEye;
        private RenderDoc _rd;

        public FrameProcessor(LedComputeShader ledComputeShader, Device d3dDevice, int numberOfLedsPerEye) {
            _computeShader = ledComputeShader.GetComputeShader(d3dDevice);
            _d3dDevice = d3dDevice;
            _numberOfLedsPerEye = numberOfLedsPerEye;

            RenderDoc.Load(out _rd);
            
            InitializeBuffers();
        }

        public byte[] FrocessFrame(Texture2D frame) {
            if (_workBuffer == null) {
                return Array.Empty<byte>();
            }
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

            // TODO: parse staging resource into led value array
            Console.WriteLine(Marshal.ReadInt32(IntPtr.Add(mapSource.DataPointer, 0)));
            _d3dDevice.ImmediateContext.UnmapSubresource(_stagingBuffer, 0);

            throw new NotImplementedException();
        }

        private void InitializeBuffers() {
            _rd?.API.StartFrameCapture(IntPtr.Zero, IntPtr.Zero);
            try {
                _workBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                    new BufferDescription(
                        NumberOfLedsPerEye * NUMBER_OF_EYES * SIZE_OF_DATAPOINT,
                        ResourceUsage.Default,
                        BindFlags.ShaderResource,
                        CpuAccessFlags.None,
                        ResourceOptionFlags.BufferStructured,
                        SIZE_OF_DATAPOINT
                    )
                );
                _workBufferUav = new UnorderedAccessView(_d3dDevice, _workBuffer);
                // staging buffer
                _stagingBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                    new BufferDescription
                    {
                        CpuAccessFlags = CpuAccessFlags.Read,
                        BindFlags = BindFlags.None,
                        SizeInBytes = NumberOfLedsPerEye * NUMBER_OF_EYES * SIZE_OF_DATAPOINT,
                        OptionFlags = ResourceOptionFlags.None,
                        Usage = ResourceUsage.Staging,
                        StructureByteStride = SIZE_OF_DATAPOINT
                    });
            } catch (Exception e) {
                Console.WriteLine("shit");
                Console.WriteLine(e);
            }
            _rd?.API.EndFrameCapture(IntPtr.Zero, IntPtr.Zero);
        }
    }
}