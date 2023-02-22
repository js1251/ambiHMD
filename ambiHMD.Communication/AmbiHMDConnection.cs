using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace ambiHMD.Communication {
    public class AmbiHMDConnection {
        private readonly SerialPort _serialPort;
        public AmbiHMDConnection(int port, int baudRate) {
            _serialPort = new SerialPort {
                PortName = $"COM{port}",
                BaudRate = baudRate
            };

            try {
                _serialPort.Open();
            } catch {
                // TODO: handle!
                return;
            }

            _lastSend = DateTime.Now;
        }

        private DateTime _lastSend;

        public Task SendMessage(string message) {
            if (!_serialPort.IsOpen) {
                return null; // TODO: handle
            }
            
            if (DateTime.Now - _lastSend < TimeSpan.FromMilliseconds(20)) {
                return null;
            }

            _lastSend = DateTime.Now;
            return Task.Run(() => {
                _serialPort.WriteLine(message);
            });
        }
    }
}
