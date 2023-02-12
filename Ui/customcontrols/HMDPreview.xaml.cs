using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ui.Annotations;

namespace Ui.customcontrols {
    public partial class HMDPreview : UserControl, INotifyPropertyChanged {
        // Resharper disable once InconsistentNaming
        private static readonly DependencyProperty _brushProperty =
            DependencyProperty.Register(nameof(PreviewBrush),
                typeof(Brush),
                typeof(HMDPreview),
                new PropertyMetadata(Brushes.WhiteSmoke));

        public Brush PreviewBrush {
            get => (Brush)GetValue(_brushProperty);
            set {
                SetValue(_brushProperty, value);
                SetLedCount();
            }
        }

        // Resharper disable once InconsistentNaming
        private static readonly DependencyProperty _ledPerEyeProperty =
            DependencyProperty.Register(nameof(LedPerEye),
                typeof(int),
                typeof(HMDPreview),
                new PropertyMetadata(6));

        public int LedPerEye {
            get => (int)GetValue(_ledPerEyeProperty);
            set {
                SetValue(_ledPerEyeProperty, value);
                SetLedCount();
            }
        }

        // Resharper disable once InconsistentNaming
        private static readonly DependencyProperty _blurProperty =
            DependencyProperty.Register(nameof(BlurPercentage),
                typeof(float),
                typeof(HMDPreview),
                new PropertyMetadata(0f));

        public float BlurPercentage {
            get => (float)GetValue(_blurProperty);
            set {
                SetValue(_blurProperty, value);
                SetLedBlur();
            }
        }

        // Resharper disable once InconsistentNaming
        private static readonly DependencyProperty _ledSizeProperty =
            DependencyProperty.Register(nameof(LedSize),
                typeof(float),
                typeof(HMDPreview),
                new PropertyMetadata(25f));

        public float LedSize {
            get => (float)GetValue(_ledSizeProperty);
            set {
                SetValue(_ledSizeProperty, value);
                SetLedSize();
            }
        }

        public HMDPreview() {
            InitializeComponent();
        }

        public void SetLedColor(int index, Color color) {
            if (index > LedPerEye - 1) {
                RightLEDs.SetColor(index - LedPerEye, color);
            } else {
                LeftLEDs.SetColor(index - LedPerEye, color);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator] protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetLedCount() {
            LeftLEDs.Amount = LedPerEye;
            RightLEDs.Amount = LedPerEye;
        }

        private void SetLedBlur() {
            var maxBlurSize = LedSize * 2f;
            // TODO: make depending on amount of leds too

            LeftLEDs.BlurRadius = maxBlurSize * BlurPercentage;
            RightLEDs.BlurRadius = maxBlurSize * BlurPercentage;
        }

        private void SetLedSize() {
            LeftLEDs.Size = LedSize;
            RightLEDs.Size = LedSize;
        }
    }
}