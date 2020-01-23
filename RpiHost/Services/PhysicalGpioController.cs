using Microsoft.Extensions.Logging;
using System.Device.Gpio;

namespace RpiHost.Services
{
    public class PhysicalGpioController : IGpioController
    {
        private GpioController controller;
        private ILogger<PhysicalGpioController> logger;

        public PhysicalGpioController(ILogger<PhysicalGpioController> logger)
        {
            controller = new GpioController(PinNumberingScheme.Logical);
            this.logger = logger;
        }

        void IGpioController.OpenPin(int gpioId)
        {
            logger.LogInformation("Request to {action} pin {pinId}", "open", gpioId);
            controller.OpenPin(gpioId);
        }

        void IGpioController.SetPinMode(int gpioId, PinMode pinMode)
        {
            logger.LogInformation("Request to {action} for pin {pinId} to {pinMode}", "set mode", gpioId, pinMode);
            controller.SetPinMode(gpioId, pinMode);
        }

        void IGpioController.Write(int gpioId, PinValue value)
        {
            logger.LogInformation("Request to {action} to pin {pinId} value {pinValue}", "write", gpioId, value);
            controller.Write(gpioId, value);
        }
    }
}
