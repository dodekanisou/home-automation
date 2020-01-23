using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Configuration
{
    public class VideoFeed
    {
        /// <summary>
        /// The unique identifier to address this stream
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// The name of the stream
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Fontawesome icon without the fa- prefix like door-open: https://fontawesome.com/icons/door-open
        /// </summary>
        public string Icon { get; set; }

        /// <summary>
        /// The URL of the input stream
        /// </summary>
        public string InputStream { get; set; }

        /// <summary>
        /// The size of the buffer to use
        /// </summary>
        public int? BufferSize {get; set; }
    }
}
