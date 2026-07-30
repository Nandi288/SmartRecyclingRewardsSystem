using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class RewardsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly RewardService _rewardService;

        public RewardsController()
        {
            _rewardService = new RewardService(_db);
        }

        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var resident = _db.Users.Find(userId);

            var rewards = _db.Rewards
                .Where(r => r.IsActive)
                .OrderBy(r => r.PointsCost)
                .ToList();

            ViewBag.Resident = resident;
            ViewBag.PointsBalance = resident?.PointsBalance ?? 0;

            return View(rewards);
        }

        // GET: /Rewards/Redeem/{id}
        public async Task<ActionResult> Redeem(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["Error"] = "Invalid reward ID.";
                return RedirectToAction("Index");
            }

            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);
            var reward = await _db.Rewards.FindAsync(id.Value);

            if (reward == null || !reward.IsActive)
            {
                TempData["Error"] = "That reward is not available.";
                return RedirectToAction("Index");
            }

            int currentBalance = user?.PointsBalance ?? 0;

            if (currentBalance < reward.PointsCost)
            {
                TempData["Error"] = $"You need {reward.PointsCost - currentBalance} more points to redeem \"{reward.Name}\".";
                return RedirectToAction("Index");
            }

            ViewBag.UserPoints = currentBalance;
            return View(reward);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ProcessRedemption(int rewardId)
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            var result = await _rewardService.ProcessRedemptionAsync(userId, rewardId);

            if (result.Success)
            {
                user.PointsBalance = result.NewBalance;
                await _db.SaveChangesAsync();

                TempData["Success"] = result.Message;
                TempData["CouponCode"] = result.CouponCode;
                return RedirectToAction("Confirmation", new { id = result.RedemptionId });
            }

            TempData["Error"] = result.Message;
            return RedirectToAction("Index");
        }

        public async Task<ActionResult> Confirmation(int? id)
        {
            if (id == null || id <= 0)
            {
                TempData["Error"] = "Invalid redemption ID.";
                return RedirectToAction("Index");
            }

            var redemption = await _db.RewardRedemptions
                .Include(r => r.Reward)
                .FirstOrDefaultAsync(r => r.RewardRedemptionId == id.Value);

            if (redemption == null)
            {
                TempData["Error"] = "Redemption not found.";
                return RedirectToAction("Index");
            }

            return View(redemption);
        }

        public async Task<ActionResult> History()
        {
            var userId = User.Identity.GetUserId();
            var redemptions = await _rewardService.GetUserRedemptionsAsync(userId);
            return View(redemptions);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}