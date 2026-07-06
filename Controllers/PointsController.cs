using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class PointsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /Points/Index
        // Shows points balance and full transaction history
        // ============================================================
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var resident = _db.Users.Find(userId);

            // Full points history ordered newest first
            var transactions = _db.PointTransactions
                .Where(t => t.UserId == userId)
                .Include(t => t.RecyclingSubmission)
                .OrderByDescending(t => t.TransactionDate)
                .ToList();

            // Summary totals
            var totalEarned = transactions
                .Where(t => t.TransactionType == TransactionType.Earned)
                .Sum(t => (int?)t.Points) ?? 0;

            var totalRedeemed = transactions
                .Where(t => t.TransactionType == TransactionType.Redeemed)
                .Sum(t => (int?)t.Points) ?? 0;

            ViewBag.Resident = resident;
            ViewBag.Transactions = transactions;
            ViewBag.TotalEarned = totalEarned;
            ViewBag.TotalRedeemed = totalRedeemed;

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}