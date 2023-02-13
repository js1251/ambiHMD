using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ui.customcontrols {
    public partial class LEDStack : UserControl {
        public float Size {
            get => _size;
            set {
                SetSize(value);
                _size = value;
            }
        }

        public float BlurRadius {
            get => _blurRadius;
            set {
                SetBlurPercentage(value);
                _blurRadius = value;
            }
        }

        public int Amount {
            get => _amount;
            set {
                SetAmount(value);
                _amount = value;
            }
        }

        public bool IsActive {
            get => _isActive;
            set {
                SetActive(value);
                _isActive = value;
            }
        }

        public bool ShowColorValue {
            get => _showColorValue;
            set {
                SetColorValueEnabled(value);
                _showColorValue = value;
            }
        }

        public ObservableCollection<LEDModel> Leds { get; }

        private float _size = 25f;
        private float _blurRadius;
        private int _amount;
        private bool _isActive;
        private bool _showColorValue;

        public LEDStack() {
            Leds = new ObservableCollection<LEDModel>();

            InitializeComponent();
        }

        public void SetColor(int index, Color color) {
            Leds[index].Brush = new SolidColorBrush(color);
        }

        private void SetBlurPercentage(float radius) {
            foreach (var led in Leds) {
                led.BlurRadius = radius;
            }
        }

        private void SetSize(float size) {
            foreach (var led in Leds) {
                led.Size = size;
            }
        }

        private void SetAmount(int amount) {
            Leds.Clear();
            for (var i = 0; i < amount; i++) {
                Leds.Add(new LEDModel {
                    BlurRadius = BlurRadius,
                    Size = Size,
                    IsActive = IsActive,
                    ShowColorValue = ShowColorValue ? Visibility.Visible : Visibility.Hidden,
                });
            }
        }

        private void SetActive(bool isActive) {
            foreach (var led in Leds) {
                led.IsActive = isActive;
            }
        }

        private void SetColorValueEnabled(bool showColorValue) {
            foreach (var led in Leds) {
                led.ShowColorValue = showColorValue ? Visibility.Visible : Visibility.Hidden;
            }
        }
    }
}