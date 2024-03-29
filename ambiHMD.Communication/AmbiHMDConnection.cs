﻿using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace ambiHMD.Communication {
    public class AmbiHMDConnection {
        private readonly SerialPort _serialPort;
        private DateTime _lastSend;
        public AmbiHMDConnection(int port, int baudRate) {
            _serialPort = new SerialPort {
                PortName = $"COM{port}",
                BaudRate = baudRate
            };

            _serialPort.Open();

            _lastSend = DateTime.Now;
        }

        public Task SendMessage(byte[] message) {
            if (!_serialPort.IsOpen) {
                return null; // TODO: handle
            }
            
            if (DateTime.Now - _lastSend < TimeSpan.FromMilliseconds(10)) {
                return null;
            }

            _lastSend = DateTime.Now;
            return Task.Run(() => {
                _serialPort.Write(message, 0, message.Length);
            });
        }
    }
}
