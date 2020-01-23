using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RpiHost.Configuration;
using RpiHost.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RpiHost.Controllers
{
    [Authorize(Policy = "motion:feed:video")]
    public class VideoFeedController : Controller
    {

        private readonly MotionStreamService streamService;
        private readonly ILogger<VideoFeedController> logger;
        private readonly Config config;

        public VideoFeedController(
            MotionStreamService streamService,
            Config config,
            ILogger<VideoFeedController> logger)
        {
            this.streamService = streamService;
            this.logger = logger;
            this.config = config;
        }

        public IActionResult Index()
        {
            return View(config.VideoFeeds);
        }

        public async Task VideoFeed(string feedId)
        {
            var videoFeed = config.VideoFeeds.Where(i => i.Id.Equals(feedId, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
            if (videoFeed != null)
            {
                if (videoFeed.BufferSize.HasValue)
                {
                    await streamService.StreamVideo(HttpContext, new Uri(videoFeed.InputStream), videoFeed.BufferSize.Value).ConfigureAwait(false);
                }
                else
                {
                    await streamService.StreamVideo(HttpContext, new Uri(videoFeed.InputStream)).ConfigureAwait(false);
                }
            }

        }
    }
}