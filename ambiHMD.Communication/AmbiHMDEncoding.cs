using System;
using System.Diagnostics;
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
                _lastColors = new (int, int, int)[_numLeds * _stride];
            }
        }

        private readonly int _stride;
        private int _numLeds;

        private (int, int, int)[] _lastColors;

        public AmbiHMDEncoder(int numLeds, int stride) {
            _stride = stride;
            NumLeds = numLeds;
        }

        public static string NullMessage() {
            return "000";
        }

        public string Encode(byte[] ledData) {
            if (ledData.Length != NumLeds * _stride) {
                throw new InvalidDataException("Invalid data length");
            }

            var stream = new MemoryStream(ledData);
            var dataPoints = ledData.Length / _stride;

            var leftEncoded = "";
            var rightEncoded = "";

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

                var bStr = b.ToString("D3");
                var gStr = g.ToString("D3");
                var rStr = r.ToString("D3");


                if (i % 2 == 0) {
                    leftEncoded += $"{rStr}{gStr}{bStr}";
                } else {
                    rightEncoded += $"{rStr}{gStr}{bStr}";
                }
            }

            _lastSmooth = DateTime.Now;

            stream.Close();

            return Brightness.ToString("D3") + leftEncoded + rightEncoded;
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