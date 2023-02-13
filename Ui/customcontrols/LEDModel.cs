using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Ui.customcontrols {
    public class LEDModel : INotifyPropertyChanged {
        public string ColorValue => IsActive ? $"{Brush.Color.R},{Brush.Color.G},{Brush.Color.B}" : "OFF";

        public bool IsActive {
            private get => _isActive;
            set {
                _isActive = value;
                OnPropertyChanged(nameof(Brush));
                OnPropertyChanged(nameof(BlurRadius));
                OnPropertyChanged(nameof(ColorValue));
            }
        }

        public Visibility ShowColorValue {
            get => _showColorValue;
            set {
                _showColorValue = value;
                OnPropertyChanged();
            }
        }

        public SolidColorBrush Brush {
            get => IsActive ? _brush : Brushes.DarkGray;
            set {
                _brush = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ColorValue));
            }
        }

        public double Size {
            get => _size;
            set {
                _size = value;
                OnPropertyChanged();
            }
        }

        public double BlurRadius {
            get => IsActive ? _blurRadius : 0d;
            set {
                _blurRadius = value;
                OnPropertyChanged();
            }
        }

        private double _blurRadius;
        private double _size = 25d;
        private SolidColorBrush _brush = Brushes.DarkGray;
        private bool _isActive;
        private Visibility _showColorValue = Visibility.Hidden;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}