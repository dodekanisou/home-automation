using Microsoft.Extensions.Logging;
using RpiHost.Configuration;
using System;
using System.Collections.Generic;
using System.Device.Gpio;
using System.Threading.Tasks;

namespace RpiHost.Services
{
    public class RelayService
    {
        private readonly Config config;
        private readonly IGpioController controller;
        private readonly ILogger<RelayService> logger;
        private readonly Dictionary<string, RelayDevice> deviceMap = new Dictionary<string, RelayDevice>();

        public RelayService(
            Config config,
            IGpioController controller,
            ILogger<RelayService> logger)
        {
            this.config = config;
            this.controller = controller;
            this.logger = logger;
            foreach (var device in this.config.RelayDevices)
            {
                InitializeRelayDevice(device);
            }
        }

        private void InitializeRelayDevice(RelayDevice device)
        {
            controller.OpenPin(device.GpioId);
            controller.SetPinMode(device.GpioId, PinMode.Output);
            // Relay needs high
            controller.Write(device.GpioId, PinValue.High);
            // Store in map for quick access
            deviceMap.Add(device.Id, device);
        }

        public async Task ActivateDevice(string deviceId)
        {
            if (!deviceMap.ContainsKey(deviceId))
            {
                throw new ApplicationException($"Unkown device with id '{deviceId}'.");
            }
            var device = deviceMap[deviceId];
            logger.LogInformation("{action} device {deviceId} - {deviceName}", "Activate", device.Id, device.Name);
            controller.Write(device.GpioId, PinValue.Low);
            await Task.Delay(device.BuzzTimeMs);
            logger.LogInformation("{action} device {deviceId} - {deviceName}", "Deactivate", device.Id, device.Name);
            controller.Write(device.GpioId, PinValue.High);
        }

        public bool DeviceExists(string deviceId)
        {
            return this.deviceMap.ContainsKey(deviceId);
        }

        public IEnumerable<RelayDevice> GetDeviceList()
        {
            return this.deviceMap.Values;
        }
    }
}
