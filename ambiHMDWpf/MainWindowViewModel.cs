using System;
using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ambiHMDWpf.MonoGameControls;
using System.Windows.Interop;
using Windows.Graphics.Capture;
using Composition.WindowsRuntimeHelpers;

namespace ambiHMDWpf {
    public class MainWindowViewModel : MonoGameViewModel {
        private SpriteBatch _spriteBatch;

        public override void LoadContent() {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        public override void Initialize() {
            base.Initialize();

            if (!GraphicsCaptureSession.IsSupported()) {
                MessageBox.Show("Graphics Capture is not supported on this device.");
                Application.Current.Shutdown();
                return;
            }

            if (Application.Current.MainWindow is null) {
                MessageBox.Show("Application.Current.MainWindow is null");
                Application.Current.Shutdown();
                return;
            }

            // https://learn.microsoft.com/en-us/answers/questions/178213/graphicscapturepicker-doesnt-work-in-wpf-net-5-pro
            // https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/tree/master/dotnet/WPF/ScreenCapture
            var picker = new GraphicsCapturePicker();
            picker.SetWindow(new WindowInteropHelper(Application.Current.MainWindow).Handle);

            var item = picker.PickSingleItemAsync().GetAwaiter().GetResult();

            if (item != null) {
                //sample.StartCaptureFromItem(item);
            }
        }

        public override void Update(GameTime gameTime) { }

        public override void Draw(GameTime gameTime) {
            GraphicsDevice.Clear(Color.Black);
        }
    }
}