using System;
using System.IO;

namespace ambiHMD.Communication {
    public class AmbiHMDEncoder {
        public int Brightness { private get; set; }
        public float GammaCorrection { private get; set; }
        public float LuminanceCorrection { private get; set; }
        public float Smoothing { private get; set; }
        private DateTime _lastSmooth;

        public int NumLeds {
            private get { return _numLeds; }
            set {
                _numLeds = value;
                _lastColors = new (int, int, int)[_numLeds * _inputStride];
            }
        }

        private readonly int _inputStride;
        private int _numLeds;

        private (int, int, int)[] _lastColors;

        public AmbiHMDEncoder(int numLeds, int inputStride) {
            _inputStride = inputStride;
            NumLeds = numLeds;
        }

        public byte[] Encode(byte[] ledData) {
            if (ledData.Length != NumLeds * _inputStride) {
                throw new InvalidDataException("Invalid data length");
            }

            var stream = new MemoryStream(ledData);
            var dataPoints = ledData.Length / _inputStride;

            var leftEncoded = new MemoryStream(new byte[NumLeds / 2 * 3]);
            var rightEncoded = new MemoryStream(new byte[NumLeds / 2 * 3]);

            var deltaTime = (DateTime.Now - _lastSmooth).TotalMilliseconds;

            for (var i = 0; i < dataPoints; i++) {
                // BGRA

                var b = stream.ReadByte();
                var g = stream.ReadByte();
                var r = stream.ReadByte();

                // alpha not used
                var _ = stream.ReadByte();

                // gamma correction
                (r, g, b) = GammaCorrect(r, g, b);

                // luminance correction
                (r, g, b) = LuminanceCorrect(r, g, b);

                // smoothing
                (r, g, b) = Smooth(i, r, g, b, deltaTime);

                // clamp max value to allow terminator 0xFF
                r = Math.Min(r, 254);
                g = Math.Min(g, 254);
                b = Math.Min(b, 254);

                var rByte = Convert.ToByte(r);
                var gByte = Convert.ToByte(g);
                var bByte = Convert.ToByte(b);

                if (i % 2 == 0) {
                    leftEncoded.WriteByte(rByte);
                    leftEncoded.WriteByte(gByte);
                    leftEncoded.WriteByte(bByte);
                } else {
                    rightEncoded.WriteByte(rByte);
                    rightEncoded.WriteByte(gByte);
                    rightEncoded.WriteByte(bByte);
                }
            }

            _lastSmooth = DateTime.Now;

            stream.Close();

            var encoded = new MemoryStream(new byte[1 + leftEncoded.Length + rightEncoded.Length + 1]);
            encoded.WriteByte(Convert.ToByte(Math.Min(Brightness, 254)));
            encoded.Write(leftEncoded.ToArray(), 0, (int)leftEncoded.Length);
            encoded.Write(rightEncoded.ToArray(), 0, (int)rightEncoded.Length);
            encoded.Write(new byte[] { 0xFF }, 0, 1);

            return encoded.ToArray();
        }

        private (int, int, int) GammaCorrect(int r, int g, int b) {
            // normalize between 0 and 255
            var bNorm = b / 255f;
            var gNorm = g / 255f;
            var rNorm = r / 255f;

            // gamma correction
            var bGamma = (float)Math.Pow(bNorm, GammaCorrection);
            var gGamma = (float)Math.Pow(gNorm, GammaCorrection);
            var rGamma = (float)Math.Pow(rNorm, GammaCorrection);

            // scale back to 0-255
            b = (int)(bGamma * 255);
            g = (int)(gGamma * 255);
            r = (int)(rGamma * 255);

            return (r, g, b);
        }

        private (int, int, int) LuminanceCorrect(int r, int g, int b) {
            var bNorm = b / 255f;
            var gNorm = g / 255f;
            var rNorm = r / 255f;

            var luminance = 0.2126 * rNorm + 0.7152 * gNorm + 0.0722 * bNorm;

            r = (int)(r * luminance * LuminanceCorrection + r * (1 - LuminanceCorrection));
            g = (int)(g * luminance * LuminanceCorrection + g * (1 - LuminanceCorrection));
            b = (int)(b * luminance * LuminanceCorrection + b * (1 - LuminanceCorrection));

            return (r, g, b);
        }

        private (int, int, int) Smooth(int index, int r, int g, int b, double deltaTime) {
            var (rLast, gLast, bLast) = _lastColors[index];

            var smoothingFactor = Math.Min(1, deltaTime / Smoothing);
            var rDiff = r - rLast;
            var gDiff = g - gLast;
            var bDiff = b - bLast;

            r = (int)(rLast + rDiff * smoothingFactor);
            g = (int)(gLast + gDiff * smoothingFactor);
            b = (int)(bLast + bDiff * smoothingFactor);

            _lastColors[index] = (r, g, b);

            return (r, g, b);
        }
    }
}