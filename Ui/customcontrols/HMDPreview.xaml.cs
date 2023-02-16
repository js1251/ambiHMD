using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using Ui.Annotations;

namespace Ui.customcontrols {
    public partial class HMDPreview : UserControl, INotifyPropertyChanged {
        public Brush PreviewBrush { get; set; } // TODO: to dependency property?

        public int LedPerEye {
            set => SetLedCount(value);
        }

        public float BlurPercentage {
            set => SetLedBlur(value);
        }

        public float LedSize {
            set => SetLedSize(value);
        }

        public bool LedActive {
            set => SetLedActive(value);
        }

        public bool ShowColorValue {
            set => SetLedShowColorvalue(value);
        }

        public bool ShowSampleAreas {
            set => SetSampleAreas(value);
        }

        public HMDPreview() {
            InitializeComponent();
        }

        public void SetLedColor(int index, Color color) {
            if (index % 2 == 0) {
                // left eye
                index /= 2;
                LeftLEDs.SetColor(index, color);
            } else {
                // right eye
                index = (index - 1) / 2;
                RightLEDs.SetColor(index, color);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void SetLedCount(int count) {
            LeftLEDs.Amount = count;
            RightLEDs.Amount = count;
        }

        private void SetLedBlur(float blurPercentage) {
            var ledSize = LeftLEDs.Size;

            var maxBlurSize = ledSize * 2f;
            // TODO: make depending on amount of leds too

            LeftLEDs.BlurRadius = maxBlurSize * blurPercentage;
            RightLEDs.BlurRadius = maxBlurSize * blurPercentage;
        }

        private void SetLedSize(float size) {
            LeftLEDs.Size = size;
            RightLEDs.Size = size;
        }

        private void SetLedActive(bool isActive) {
            LeftLEDs.IsActive = isActive;
            RightLEDs.IsActive = isActive;
        }

        private void SetLedShowColorvalue(bool showColorvalue) {
            LeftLEDs.ShowColorValue = showColorvalue;
            RightLEDs.ShowColorValue = showColorvalue;
        }

        private void SetSampleAreas(bool showSampleAreas) {
            // TODO: implement
        }
    }
}