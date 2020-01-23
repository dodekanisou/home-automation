using System.Collections.Generic;

namespace RpiHost.Configuration
{
    public class Config
    {
        public List<RelayDevice> RelayDevices { get; set; }

        public List<VideoFeed> VideoFeeds { get; set; }
    }
}
