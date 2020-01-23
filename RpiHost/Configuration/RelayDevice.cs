namespace RpiHost.Configuration
{
    public class RelayDevice
    {
        /// <summary>
        /// The unique identifier to address this device
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the connected device
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The pin that controls the specific's device relay
        /// </summary>
        public int GpioId { get; set; }

        /// <summary>
        /// Time in ms to keep the relay open for this device
        /// </summary>
        public int BuzzTimeMs { get; set; }

        /// <summary>
        /// Fontawesome icon without the fa- prefix like door-open: https://fontawesome.com/icons/door-open
        /// </summary>
        public string Icon { get; set; }
    }
}
