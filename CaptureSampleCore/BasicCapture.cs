//  ---------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
// 
//  The MIT License (MIT)
// 
//  Permission is hereby granted, free of charge, to any person obtaining a copy
//  of this software and associated documentation files (the "Software"), to deal
//  in the Software without restriction, including without limitation the rights
//  to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
//  copies of the Software, and to permit persons to whom the Software is
//  furnished to do so, subject to the following conditions:
// 
//  The above copyright notice and this permission notice shall be included in
//  all copies or substantial portions of the Software.
// 
//  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
//  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
//  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
//  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
//  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
//  OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
//  THE SOFTWARE.
//  ---------------------------------------------------------------------------------

using Composition.WindowsRuntimeHelpers;
using SharpDX.Direct3D11;
using System;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI.Composition;
using SharpDX.DXGI;

namespace CaptureSampleCore {
    public sealed class BasicCapture : IDisposable {
        private readonly GraphicsCaptureItem _item;
        private readonly Direct3D11CaptureFramePool _framePool;
        private readonly GraphicsCaptureSession _session;
        private SizeInt32 _lastSize;

        private readonly IDirect3DDevice _device;
        private readonly SharpDX.Direct3D11.Device _d3dDevice;
        private readonly SwapChain1 _swapChain;

        public BasicCapture(IDirect3DDevice d, GraphicsCaptureItem i) {
            _item = i;
            _device = d;
            _d3dDevice = Direct3D11Helper.CreateSharpDXDevice(_device);

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

            _swapChain = new SwapChain1(dxgiFactory, _d3dDevice, ref description);

            _framePool =
                Direct3D11CaptureFramePool.Create(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, i.Size);
            _session = _framePool.CreateCaptureSession(i);
            _lastSize = i.Size;

            _framePool.FrameArrived += OnFrameArrived;
        }

        public void Dispose() {
            _session?.Dispose();
            _framePool?.Dispose();
            _swapChain?.Dispose();
            _d3dDevice?.Dispose();
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
                    _swapChain.ResizeBuffers(2,
                        _lastSize.Width,
                        _lastSize.Height,
                        Format.B8G8R8A8_UNorm,
                        SwapChainFlags.None);
                }

                using (var backBuffer = _swapChain.GetBackBuffer<Texture2D>(0))
                using (var bitmap = Direct3D11Helper.CreateSharpDXTexture2D(frame.Surface)) {
                    _d3dDevice.ImmediateContext.CopyResource(bitmap, backBuffer);

                    
                    // TODO: Look into this!!
                    // Use this bitmap here to feed into computeshader using SharpDX
                    // read returned bitmap (only containing rgb values)
                    // https://stackoverflow.com/questions/44345239/running-a-dx11-compute-shader-with-sharpdx-cannot-get-results

                    // ======== DO ALL THIS ONLY ONCE ========

                    // compile shader bytecode
                    var compilationResult =
                        ShaderBytecode.CompileFromFile("test.hlsl", "main", "cs_5_0", ShaderFlags.Debug);

                    // create compute shader
                    var computeShader = new ComputeShader(_d3dDevice, compilationResult.Bytecode);

                    // created access view so shader has access to current frame capture
                    var view = new UnorderedAccessView(_d3dDevice,
                        bitmap,
                        new UnorderedAccessViewDescription {
                            Format = Format.R8G8B8A8_UNorm,
                            Dimension = UnorderedAccessViewDimension.Texture2D,
                            Texture2D = { MipSlice = 0 }
                        });

                    /*
                    // TODO: Does it have to be a texture? Can it be a buffer?
                    var stagingTexture = new Texture2D(_d3dDevice,
                        new Texture2DDescription {
                            CpuAccessFlags = CpuAccessFlags.Read,
                            BindFlags = BindFlags.None,
                            Format = Format.R8G8B8A8_UNorm,
                            Width = 16, // TODO: number of LEDs
                            Height = 1,
                            OptionFlags = ResourceOptionFlags.None,
                            MipLevels = 1,
                            ArraySize = 1,
                            SampleDescription = { Count = 1, Quality = 0 },
                            Usage = ResourceUsage.Staging
                        });
                    */
                    
                    // staging buffer
                    var stagingBuffer = new SharpDX.Direct3D11.Buffer(_d3dDevice,
                        new BufferDescription {
                            CpuAccessFlags = CpuAccessFlags.Read,
                            BindFlags = BindFlags.None,
                            SizeInBytes = 16 * 4, // TODO: number of LEDs are variable
                            OptionFlags = ResourceOptionFlags.None,
                            Usage = ResourceUsage.Staging
                        });

                    // =======================================

                    // set the shader
                    _d3dDevice.ImmediateContext.ComputeShader.Set(computeShader);

                    // give it access to the view
                    _d3dDevice.ImmediateContext.ComputeShader.SetUnorderedAccessView(0, view);

                    // send it off to run
                    _d3dDevice.ImmediateContext.Dispatch(32, 32, 1);

                    // copy the results into staging resource
                    _d3dDevice.ImmediateContext.CopyResource(bitmap, stagingBuffer);

                    // get access to the staging resource on CPU
                    var mapSource = _d3dDevice.ImmediateContext.MapSubresource(stagingBuffer,
                        0,
                        MapMode.Read,
                        SharpDX.Direct3D11.MapFlags.None);

                    // TODO: parse staging resource into led value array
                    Console.WriteLine(Marshal.ReadInt32(IntPtr.Add(mapSource.DataPointer, 0)));
                }
            }

            _swapChain.Present(0, PresentFlags.None);

            if (newSize) {
                _framePool.Recreate(_device, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _lastSize);
            }
        }
    }
}