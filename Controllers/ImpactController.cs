using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    // UC-10: Personal Environmental Impact Report (Resident)
    [Authorize(Roles = "Resident")]
    public class ImpactController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: /Impact/Index
        public ActionResult Index()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            // ── All verified submissions for this resident ─────────
            var verified = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == userId && s.Status == SubmissionStatus.Verified)
                .Include(s => s.MaterialType)
                .ToList();

            // ── Overall totals ─────────────────────────────────────
            var totalWeightKg = verified.Sum(s => s.WeightKg);
            var totalCo2Kg = verified.Sum(s => s.CO2SavedKg);
            var totalPoints = verified.Sum(s => s.PointsAwarded);
            var totalSubmissions = verified.Count;

            // ── CO2 equivalent comparisons ─────────────────────────
            // Average car emits ~0.21 kg CO2 per km
            var carKmEquivalent = totalCo2Kg > 0
                ? Math.Round(totalCo2Kg / 0.21m, 1) : 0;

            // Average tree absorbs ~21 kg CO2 per year
            var treesEquivalent = totalCo2Kg > 0
                ? Math.Round(totalCo2Kg / 21m, 2) : 0;

            // ── Breakdown by material type ─────────────────────────
            var byMaterial = verified
                .GroupBy(s => s.MaterialType.Name)
                .Select(g => new MaterialBreakdown
                {
                    MaterialName = g.Key,
                    ColourCode = g.First().MaterialType.ColourCode,
                    TotalWeightKg = g.Sum(s => s.WeightKg),
                    TotalCo2Kg = g.Sum(s => s.CO2SavedKg),
                    TotalPoints = g.Sum(s => s.PointsAwarded),
                    Count = g.Count()
                })
                .OrderByDescending(x => x.TotalWeightKg)
                .ToList();

            // ── Monthly breakdown (last 12 months) ────────────────
            var twelveMonthsAgo = DateTime.Now.AddMonths(-12);
            var byMonth = verified
                .Where(s => s.SubmissionDate >= twelveMonthsAgo)
                .GroupBy(s => new { s.SubmissionDate.Year, s.SubmissionDate.Month })
                .Select(g => new MonthlyBreakdown
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    WeightKg = g.Sum(s => s.WeightKg),
                    Co2Kg = g.Sum(s => s.CO2SavedKg),
                    Points = g.Sum(s => s.PointsAwarded),
                    Count = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToList();

            // ── Leaderboard rank ──────────────────────────────────
            var allRanks = _db.Users
                .Where(u => u.Role == "Resident")
                .OrderByDescending(u => u.PointsBalance)
                .Select(u => u.Id)
                .ToList();
            var rank = allRanks.IndexOf(userId) + 1;

            var vm = new ImpactViewModel
            {
                User = user,
                TotalWeightKg = totalWeightKg,
                TotalCo2Kg = totalCo2Kg,
                TotalPoints = totalPoints,
                TotalSubmissions = totalSubmissions,
                CarKmEquivalent = carKmEquivalent,
                TreesEquivalent = treesEquivalent,
                ByMaterial = byMaterial,
                ByMonth = byMonth,
                LeaderboardRank = rank
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }

    // ── ViewModels used by Impact views ──────────────────────────────
    public class ImpactViewModel
    {
        public ApplicationUser User { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal TotalCo2Kg { get; set; }
        public int TotalPoints { get; set; }
        public int TotalSubmissions { get; set; }
        public decimal CarKmEquivalent { get; set; }
        public decimal TreesEquivalent { get; set; }
        public List<MaterialBreakdown> ByMaterial { get; set; }
        public List<MonthlyBreakdown> ByMonth { get; set; }
        public int LeaderboardRank { get; set; }
    }

    public class MaterialBreakdown
    {
        public string MaterialName { get; set; }
        public string ColourCode { get; set; }
        public decimal TotalWeightKg { get; set; }
        public decimal TotalCo2Kg { get; set; }
        public int TotalPoints { get; set; }
        public int Count { get; set; }
    }

    public class MonthlyBreakdown
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal WeightKg { get; set; }
        public decimal Co2Kg { get; set; }
        public int Points { get; set; }
        public int Count { get; set; }
        public string MonthLabel
        {
            get { return new DateTime(Year, Month, 1).ToString("MMM yyyy"); }
        }
    }
}