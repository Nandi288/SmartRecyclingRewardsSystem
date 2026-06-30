using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Dashboard");

            return View();
        }
    }
}
