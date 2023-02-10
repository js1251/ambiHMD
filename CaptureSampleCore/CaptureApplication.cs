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

using System;
using System.IO;
using System.Numerics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Composition.WindowsRuntimeHelpers;
using FrameProcessing;
using SharpDX.Direct3D11;

namespace CaptureCore {
    public sealed class CaptureApplication : IDisposable {
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

        private readonly LedComputeShader _ledShader;
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
            _content.AnchorPoint = new Vector2(0.5f);
            _content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
            _content.RelativeSizeAdjustment = Vector2.One;
            _content.Size = new Vector2(-80, -80);
            _content.Brush = _brush;
            _content.Shadow = shadow;
            _root.Children.InsertAtTop(_content);

            _ledShader = new LedComputeShader(new FileInfo("example.hlsl"));
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
            var ledValueData = _frameProcessor.FrocessFrame(texture);

            // TODO: this probably introduces a lot of latency
            for (var i = 0; i < NumberOfLedsPerEye * FrameProcessor.NUMBER_OF_EYES; i++) {
                var color = GetColor(ledValueData, i);
                // TODO: update preview led color
            }

            // TODO: send to Arduino api
        }

        private SolidColorBrush GetColor(byte[] ledValueData, int index) {
            var singleLedData = new byte[FrameProcessor.SIZE_OF_DATAPOINT];
            ledValueData.CopyTo(singleLedData, FrameProcessor.SIZE_OF_DATAPOINT * index);
            var color = Color.FromArgb(singleLedData[3], singleLedData[0], singleLedData[1], singleLedData[2]);
            return new SolidColorBrush(color);
        }
    }
}