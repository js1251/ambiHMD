using System.Windows.Media;

namespace Ui.customcontrols {
    public class LEDModel {
        public string ColorString => GetColorString();
        public SolidColorBrush Brush { get; set; } = Brushes.Orange;
        public double Size { get; set; } = 25d;
        public double BlurRadius { get; set; } = 0d;

        private string GetColorString() {
            return $"{Brush.Color.R}, {Brush.Color.G}, {Brush.Color.B}";
        }
    }
}