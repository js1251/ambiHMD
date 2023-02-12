namespace FrameProcessing {
    public class LedData {
        private readonly byte[] _data;
        private readonly int _numberOfLeds;
        private readonly int _stride;

        public byte[] UnPadded { get; }

        public LedData(byte[] data, int numberOfLeds, int stride) {
            _data = data;
            _numberOfLeds = numberOfLeds;
            _stride = stride;

            UnPadded = GetUnpadded();
        }

        private byte[] GetUnpadded() {
            var unpadded = new byte[_numberOfLeds * 4]; // RGBA

            var unpaddedIndex = 0;
            for (var i = 0; i < _numberOfLeds; i += _stride) {
                unpadded[unpaddedIndex++] = _data[i];
                unpadded[unpaddedIndex++] = _data[i + 1];
                unpadded[unpaddedIndex++] = _data[i + 2];
                unpadded[unpaddedIndex++] = _data[i + 3];
            }

            return unpadded;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ledIndex">a, r, g, b</param>
        /// <returns></returns>
        public (byte, byte, byte, byte) GetColor(int ledIndex) {
            var index = ledIndex * FrameProcessor.DATA_STRIDE;
            return (_data[index + 0], _data[index + 1], _data[index + 2], _data[index + 3]);
        }
    }
}