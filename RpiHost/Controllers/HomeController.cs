using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RpiHost.Controllers
{
    [Authorize(Policy = "use:webui")]
    public class HomeController : Controller
    {
        public IActionResult IndexAsync()
        {
            return View();
        }

        public IActionResult Relays()
        {
            return View();
        }

    }
}