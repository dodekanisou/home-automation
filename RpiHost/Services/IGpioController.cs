using System.Device.Gpio;

namespace RpiHost.Services
{
    public interface IGpioController
    {
        internal void OpenPin(int gpioId);

        internal void SetPinMode(int gpioId, PinMode output);

        internal void Write(int gpioId, PinValue high);
    }
}
