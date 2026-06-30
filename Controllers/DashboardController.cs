using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        public ActionResult Index()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Admin");
            if (User.IsInRole("CollectionOfficer")) return RedirectToAction("Officer");
            return RedirectToAction("Resident");
        }

        [Authorize(Roles = "Resident")]
        public ActionResult Resident()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            var recentSubmissions = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == userId)
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .OrderByDescending(s => s.SubmissionDate)
                .Take(5).ToList();

            var totalEarned = _db.PointTransactions
                .Where(t => t.UserId == userId && t.TransactionType == TransactionType.Earned)
                .Select(t => (int?)t.Points).Sum() ?? 0;

            var totalWeight = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == userId && s.Status == SubmissionStatus.Verified)
                .Select(s => (decimal?)s.WeightKg).Sum() ?? 0;

            var totalCo2 = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == userId && s.Status == SubmissionStatus.Verified)
                .Select(s => (decimal?)s.CO2SavedKg).Sum() ?? 0;

            var pendingCount = _db.RecyclingSubmissions
                .Count(s => s.ResidentId == userId && s.Status == SubmissionStatus.Pending);

            var unreadCount = _db.Notifications
                .Count(n => n.UserId == userId && !n.IsRead);

            var allBalances = _db.Users
                .Where(u => u.Role == "Resident")
                .OrderByDescending(u => u.PointsBalance)
                .Select(u => u.Id).ToList();
            var rank = allBalances.IndexOf(userId) + 1;

            var recentNotifications = _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(3).ToList();

            ViewBag.User = user;
            ViewBag.RecentSubmissions = recentSubmissions;
            ViewBag.TotalEarned = totalEarned;
            ViewBag.TotalWeight = totalWeight;
            ViewBag.TotalCo2 = totalCo2;
            ViewBag.PendingCount = pendingCount;
            ViewBag.UnreadCount = unreadCount;
            ViewBag.Rank = rank;
            ViewBag.RecentNotifications = recentNotifications;

            return View();
        }

        [Authorize(Roles = "CollectionOfficer")]
        public ActionResult Officer()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            var assignedPoints = _db.DropOffPoints
                .Where(d => d.AssignedOfficerId == userId && d.IsActive).ToList();
            var assignedPointIds = assignedPoints.Select(d => d.DropOffPointId).ToList();

            var pendingSubmissions = _db.RecyclingSubmissions
                .Where(s => assignedPointIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Pending)
                .Include(s => s.Resident).Include(s => s.MaterialType).Include(s => s.DropOffPoint)
                .OrderByDescending(s => s.SubmissionDate).Take(10).ToList();

            var totalVerified = _db.RecyclingSubmissions.Count(s =>
                assignedPointIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Verified && s.VerifiedByOfficerId == userId);

            var totalRejected = _db.RecyclingSubmissions.Count(s =>
                assignedPointIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Rejected && s.VerifiedByOfficerId == userId);

            var pendingCount = _db.RecyclingSubmissions.Count(s =>
                assignedPointIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Pending);

            var today = DateTime.Today;
            var todayVerified = _db.RecyclingSubmissions.Count(s =>
                assignedPointIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Verified &&
                s.VerifiedByOfficerId == userId && DbFunctions.TruncateTime(s.ProcessedAt) == today);

            ViewBag.User = user;
            ViewBag.AssignedPoints = assignedPoints;
            ViewBag.PendingSubmissions = pendingSubmissions;
            ViewBag.TotalVerified = totalVerified;
            ViewBag.TotalRejected = totalRejected;
            ViewBag.PendingCount = pendingCount;
            ViewBag.TodayVerified = todayVerified;

            return View();
        }

        [Authorize(Roles = "Admin")]
        public ActionResult Admin()
        {
            var totalResidents = _db.Users.Count(u => u.Role == "Resident");
            var totalOfficers = _db.Users.Count(u => u.Role == "CollectionOfficer");
            var totalSubmissions = _db.RecyclingSubmissions.Count();
            var totalVerified = _db.RecyclingSubmissions.Count(s => s.Status == SubmissionStatus.Verified);

            var totalWeightKg = _db.RecyclingSubmissions
                .Where(s => s.Status == SubmissionStatus.Verified)
                .Select(s => (decimal?)s.WeightKg).Sum() ?? 0;

            var totalCo2 = _db.RecyclingSubmissions
                .Where(s => s.Status == SubmissionStatus.Verified)
                .Select(s => (decimal?)s.CO2SavedKg).Sum() ?? 0;

            var byMaterial = _db.RecyclingSubmissions
                .Where(s => s.Status == SubmissionStatus.Verified)
                .Include(s => s.MaterialType)
                .GroupBy(s => s.MaterialType.Name)
                .Select(g => new { Material = g.Key, Weight = g.Sum(s => s.WeightKg) })
                .ToList();

            var sixMonthsAgo = DateTime.Now.AddMonths(-6);
            var byMonth = _db.RecyclingSubmissions
                .Where(s => s.SubmissionDate >= sixMonthsAgo)
                .GroupBy(s => new { s.SubmissionDate.Year, s.SubmissionDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToList();

            var topResidents = _db.Users
                .Where(u => u.Role == "Resident")
                .OrderByDescending(u => u.PointsBalance)
                .Take(5).ToList();

            var recentSubmissions = _db.RecyclingSubmissions
                .Include(s => s.Resident).Include(s => s.MaterialType).Include(s => s.DropOffPoint)
                .OrderByDescending(s => s.SubmissionDate).Take(5).ToList();

            var pendingCount = _db.RecyclingSubmissions.Count(s => s.Status == SubmissionStatus.Pending);

            ViewBag.TotalResidents = totalResidents;
            ViewBag.TotalOfficers = totalOfficers;
            ViewBag.TotalSubmissions = totalSubmissions;
            ViewBag.TotalVerified = totalVerified;
            ViewBag.TotalWeightKg = totalWeightKg;
            ViewBag.TotalCo2 = totalCo2;
            ViewBag.PendingCount = pendingCount;
            ViewBag.TopResidents = topResidents;
            ViewBag.RecentSubmissions = recentSubmissions;

            ViewBag.ChartMaterialLabels = byMaterial.Select(x => x.Material).ToList();
            ViewBag.ChartMaterialWeights = byMaterial.Select(x => x.Weight).ToList();
            ViewBag.ChartMonthLabels = byMonth.Select(x => new DateTime(x.Year, x.Month, 1).ToString("MMM yyyy")).ToList();
            ViewBag.ChartMonthCounts = byMonth.Select(x => x.Count).ToList();

            return View();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}
