using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using Ui.Annotations;

namespace Ui.customcontrols {
    public partial class LEDStack : UserControl, INotifyPropertyChanged {
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

        public ObservableCollection<LEDModel> Leds { get; }

        private float _size;
        private float _blurRadius;
        private int _amount;

        public LEDStack() {
            Leds = new ObservableCollection<LEDModel>();

            InitializeComponent();
        }
        
        public void SetColor(int index, Color color) {
            Leds[index].Brush = new SolidColorBrush(color);
        }

        private void SetBlurPercentage(float percentage) {
            foreach (var led in Leds) {
                led.BlurRadius = led.Size * 2f * percentage;
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
                });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator] protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}