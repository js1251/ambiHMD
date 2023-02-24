using System.IO;

namespace ambiHMD.Communication {
    public class AmbiHMDEncoder {
        public int Brightness { private get; set; }
        public float GammaCorrection { private get; set; }
        public float Smoothing { private get; set; }

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
            for (var i = 0; i < dataPoints; i++) {
                // BGRA

                var b = stream.ReadByte();
                var g = stream.ReadByte();
                var r = stream.ReadByte();

                // alpha not used
                var _ = stream.ReadByte();

                // gamma correction
                (r, g, b) = GammaMethod1(r, g, b);

                // smoothing
                (r, g, b) = Smooth(i, r, g, b);

                var bStr = b.ToString("D3");
                var gStr = g.ToString("D3");
                var rStr = r.ToString("D3");


                if (i % 2 == 0) {
                    leftEncoded += $"{rStr}{gStr}{bStr}";
                } else {
                    rightEncoded += $"{rStr}{gStr}{bStr}";
                }
            }

            stream.Close();

            return Brightness.ToString("D3") + leftEncoded + rightEncoded;
        }

        private (int, int, int) GammaMethod1(int r, int g, int b) {
            // normalize between 0 and 255
            var b_norm = b / 255f;
            var g_norm = g / 255f;
            var r_norm = r / 255f;

            // gamma correction
            var b_gamma = (float)System.Math.Pow(b_norm, GammaCorrection);
            var g_gamma = (float)System.Math.Pow(g_norm, GammaCorrection);
            var r_gamma = (float)System.Math.Pow(r_norm, GammaCorrection);

            // scale back to 0-255
            b = (int)(b_gamma * 255);
            g = (int)(g_gamma * 255);
            r = (int)(r_gamma * 255);

            return (r, g, b);
        }

        private (int, int, int) GammaMethod2(int r, int g, int b) {
            var b_norm = b / 255f;
            var g_norm = g / 255f;
            var r_norm = r / 255f;

            var adjustment = (0.2126 * r_norm + 0.7152 * g_norm + 0.0722 * b_norm);

            r = (int)(r * adjustment);
            g = (int)(g * adjustment);
            b = (int)(b * adjustment);

            return (r, g, b);
        }

        private (int, int, int) Smooth(int index, int r, int g, int b) {
            var (rLast, gLast, bLast) = _lastColors[index];

            // linear interpolation
            r = (int)(r * Smoothing + rLast * (1 - Smoothing));
            g = (int)(g * Smoothing + gLast * (1 - Smoothing));
            b = (int)(b * Smoothing + bLast * (1 - Smoothing));

            _lastColors[index] = (r, g, b);

            return (r, g, b);
        }
    }
}