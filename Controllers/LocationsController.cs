using SmartRecyclingRewardsSystem.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class LocationsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /Locations/Index
        // Shows all active drop-off points for residents
        // ============================================================
        public ActionResult Index()
        {
            var points = _db.DropOffPoints
                .Where(d => d.IsActive)
                .Include(d => d.AssignedOfficer)
                .OrderBy(d => d.City).ThenBy(d => d.Name)
                .ToList();

            return View(points);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}