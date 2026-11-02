using Microsoft.AspNetCore.Mvc;

namespace ShopProject.Controllers
{
    public class LegalController : Controller
    {
        [Route("legal/oferta")]
        public IActionResult Oferta()
        {
            return View();
        }

        [Route("legal/refund")]
        public IActionResult Refund()
        {
            return View();
        }

        [Route("legal/privacy")]
        public IActionResult Privacy()
        {
            return View();
        }
    }
}