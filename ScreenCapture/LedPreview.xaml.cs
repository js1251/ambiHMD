using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WPFCaptureSample
{
    public partial class LedPreview : UserControl
    {
        public static readonly DependencyProperty SizeProperty =
            DependencyProperty.Register("Size",
                typeof(int),
                typeof(LedPreview),
                new PropertyMetadata(25));

        public SolidColorBrush Size
        {
            get => (SolidColorBrush)GetValue(SizeProperty);
            set => SetValue(SizeProperty, value);
        }

        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color",
                typeof(SolidColorBrush),
                typeof(LedPreview),
                new PropertyMetadata(Brushes.Red));

        public SolidColorBrush Color
        {
            get => (SolidColorBrush)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        public LedPreview()
        {
            InitializeComponent();
        }
    }
}