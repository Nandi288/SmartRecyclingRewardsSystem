using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class RewardsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /Rewards/Index
        // Lists active rewards + current points balance
        // ============================================================
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var resident = _db.Users.Find(userId);

            var rewards = _db.Rewards
                .Where(r => r.IsActive)
                .OrderBy(r => r.PointsCost)
                .ToList();

            ViewBag.Resident = resident;
            ViewBag.PointsBalance = GetPointsBalance(userId);

            return View(rewards);
        }

        // ============================================================
        // POST: /Rewards/Redeem
        // Deducts points and logs a PointTransaction + RewardRedemption
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Redeem(int id)
        {
            var userId = User.Identity.GetUserId();
            var reward = _db.Rewards.Find(id);

            if (reward == null || !reward.IsActive)
            {
                TempData["Error"] = "That reward is not available.";
                return RedirectToAction("Index");
            }

            int currentBalance = GetPointsBalance(userId);

            if (currentBalance < reward.PointsCost)
            {
                TempData["Error"] = $"You need {reward.PointsCost - currentBalance} more points to redeem \"{reward.Name}\".";
                return RedirectToAction("Index");
            }

            int newBalance = currentBalance - reward.PointsCost;

            using (var dbTransaction = _db.Database.BeginTransaction())
            {
                try
                {
                    _db.PointTransactions.Add(new PointTransaction
                    {
                        UserId = userId,
                        TransactionType = TransactionType.Redeemed,
                        Points = reward.PointsCost,
                        BalanceAfter = newBalance,
                        Description = $"Redeemed: {reward.Name}",
                        TransactionDate = DateTime.Now
                    });

                    _db.RewardRedemptions.Add(new RewardRedemption
                    {
                        UserId = userId,
                        RewardId = reward.RewardId,
                        PointsSpent = reward.PointsCost,
                        RedemptionDate = DateTime.Now
                    });

                    _db.SaveChanges();
                    dbTransaction.Commit();

                    TempData["Success"] = $"You redeemed \"{reward.Name}\" for {reward.PointsCost} points!";
                }
                catch
                {
                    dbTransaction.Rollback();
                    TempData["Error"] = "Something went wrong while redeeming. Please try again.";
                }
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // Recalculates balance the same way PointsController would show it
        // ============================================================
        private int GetPointsBalance(string userId)
        {
            var last = _db.PointTransactions
                .Where(t => t.UserId == userId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.PointTransactionId)
                .FirstOrDefault();

            return last?.BalanceAfter ?? 0;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}