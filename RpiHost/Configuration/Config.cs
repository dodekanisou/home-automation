using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Configuration
{
    public class Config
    {
        public List<RelayDevice> RelayDevices { get; set; }

        public List<VideoFeed> VideoFeeds { get; set; }
    }
}
