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

                var b = stream.ReadByte().ToString("D3");
                var g = stream.ReadByte().ToString("D3");
                var r = stream.ReadByte().ToString("D3");

                // alpha not used
                var _ = stream.ReadByte().ToString("D3");

                if (i % 2 == 0) {
                    leftEncoded += $"{r}{g}{b}";
                } else {
                    rightEncoded += $"{r}{g}{b}";
                }
            }

            stream.Close();

            return brightness.ToString("D3") + leftEncoded + rightEncoded;
        }
    }
}