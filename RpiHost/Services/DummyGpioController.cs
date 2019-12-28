using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Services
{
    public class DummyGpioController : IGpioController
    {
        private ILogger<DummyGpioController> logger;

        public DummyGpioController(ILogger<DummyGpioController> logger)
        {
            this.logger = logger;
        }

        void IGpioController.OpenPin(int gpioId)
        {
            logger.LogInformation("Request to {action} pin {pinId}", "open", gpioId);
        }

        void IGpioController.SetPinMode(int gpioId, PinMode pinMode)
        {
            logger.LogInformation("Request to {action} for pin {pinId} to {pinMode}", "set mode", gpioId, pinMode);
        }

        void IGpioController.Write(int gpioId, PinValue value)
        {
            logger.LogInformation("Request to {action} to pin {pinId} value {pinValue}", "write", gpioId, value);
        }
    }
}
