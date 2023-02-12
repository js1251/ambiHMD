using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Ui.customcontrols {
    public partial class LedPreview : UserControl {
        // Resharper disable once InconsistentNaming
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size",
                typeof(float),
                typeof(LedPreview),
                new PropertyMetadata(25f));

        public float Size {
            get => (float)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        // Resharper disable once InconsistentNaming
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color",
                typeof(SolidColorBrush),
                typeof(LedPreview),
                new PropertyMetadata(Brushes.Orange));

        public SolidColorBrush Color {
            get => (SolidColorBrush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        // Resharper disable once InconsistentNaming
        public static readonly DependencyProperty BlurProperty =
            DependencyProperty.Register("Blur",
                typeof(float),
                typeof(LedPreview),
                new PropertyMetadata(1f));

        public float Blur {
            get => (float)GetValue(BlurProperty);
            set {
                if (value < 0 || value > 1) {
                    throw new ArgumentOutOfRangeException(nameof(value), "Value must be between 0 and 1.");
                }

                var maxBlurSize = Size * 2f;
                SetValue(BlurProperty, maxBlurSize * value);
            }
        }

        public LedPreview() {
            InitializeComponent();
        }
    }
}