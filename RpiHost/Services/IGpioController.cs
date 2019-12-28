using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Services
{
    public interface IGpioController
    {
        internal void OpenPin(int gpioId);

        internal void SetPinMode(int gpioId, PinMode output);

        internal void Write(int gpioId, PinValue high);
    }
}
