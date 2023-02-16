using System;

namespace CaptureCore {
    public class LedData {
        private readonly int _stride;
        private readonly int _numberOfLeds;

        public byte[] Data { get; }

        public LedData(int numberOfLeds, int stride) {
            _numberOfLeds = numberOfLeds;
            _stride = stride;

            Data = new byte[numberOfLeds * stride];
        }

        public void SetData(int index, byte[] data) {
            if (index >= _numberOfLeds || index < 0) {
                throw new ArgumentOutOfRangeException(nameof(index),
                    "index must be above 0 and less than number of leds");
            }

            if (data.Length != _stride) {
                throw new ArgumentException("data length must be equal to stride", nameof(data));
            }

            var dataOffset = index * _stride;
            for (var i = 0; i < _stride; i++) {
                Data[dataOffset + i] = data[i];
            }
        }

        public byte[] GetData(int index) {
            var offset = index * _stride;
            var data = new byte[_stride];
            for (var i = 0; i < _stride; i++) {
                data[i] = Data[offset + i];
            }

            return data;
        }
    }
}