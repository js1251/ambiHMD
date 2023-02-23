using System.IO;

namespace ambiHMD.Communication {
    public static class AmbiHMDEncoding {
        public static string Encode(int brightness, byte[] ledData, int stride) {
            var stream = new MemoryStream(ledData);
            var dataPoints = ledData.Length / stride;

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
                (r, g, b) = GammaMethod2(r, g, b);

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

            return brightness.ToString("D3") + leftEncoded + rightEncoded;
        }

        private static (int, int, int) GammaMethod1(int r, int g, int b) {
            // normalize between 0 and 255
            var b_norm = b / 255f;
            var g_norm = g / 255f;
            var r_norm = r / 255f;

            // gamma correction
            var b_gamma = (float)System.Math.Pow(b_norm, 2); // TODO: introduce slider for this!
            var g_gamma = (float)System.Math.Pow(g_norm, 2);
            var r_gamma = (float)System.Math.Pow(r_norm, 2);

            // scale back to 0-255
            b = (int)(b_gamma * 255);
            g = (int)(g_gamma * 255);
            r = (int)(r_gamma * 255);

            return (r, g, b);
        }

        private static (int, int, int) GammaMethod2(int r, int g, int b) {
            var b_norm = b / 255f;
            var g_norm = g / 255f;
            var r_norm = r / 255f;

            var adjustment = (0.2126 * r_norm + 0.7152 * g_norm + 0.0722 * b_norm);

            r = (int)(r * adjustment);
            g = (int)(g * adjustment);
            b = (int)(b * adjustment);

            return (r, g, b);
        }
    }
}