using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SmartRecyclingRewardsSystem.ViewModels;


namespace SmartRecyclingRewardsSystem.Services
    {
        public class BadgeService
        {
            private readonly ApplicationDbContext db;

            public BadgeService()
            {
                db = new ApplicationDbContext();
            }

            public List<Badge> CheckAndAwardBadges(string residentId)
            {
                var newlyAwarded = new List<Badge>();

                var verifiedSubmissions = db.RecyclingSubmissions
                    .Where(s => s.ResidentId == residentId && s.Status == SubmissionStatus.Verified)
                    .ToList();

                if (!verifiedSubmissions.Any())
                    return newlyAwarded;

                var alreadyEarnedBadgeIds = db.UserBadges
                    .Where(ub => ub.UserId == residentId)
                    .Select(ub => ub.BadgeId)
                    .ToList();

                var allBadges = db.Badges.ToList();

                TryAward(allBadges, "First Drop", alreadyEarnedBadgeIds, residentId,
                    verifiedSubmissions.Count >= 1, newlyAwarded);

                var totalWeight = verifiedSubmissions.Sum(s => s.WeightKg);
                TryAward(allBadges, "100kg Club", alreadyEarnedBadgeIds, residentId,
                    totalWeight >= 100, newlyAwarded);

                var hasEWaste = verifiedSubmissions
                    .Any(s => s.MaterialType != null && s.MaterialType.Name == "E-Waste");
                TryAward(allBadges, "E-Waste Hero", alreadyEarnedBadgeIds, residentId,
                    hasEWaste, newlyAwarded);

                var topResidentId = db.Users
                    .Where(u => u.Role == "Resident" && u.IsActive)
                    .OrderByDescending(u => u.PointsBalance)
                    .Select(u => u.Id)
                    .FirstOrDefault();
                TryAward(allBadges, "Points Champion", alreadyEarnedBadgeIds, residentId,
                    topResidentId == residentId, newlyAwarded);

                var hasFiveWeekStreak = CheckFiveWeekStreak(verifiedSubmissions);
                TryAward(allBadges, "5-Week Streak", alreadyEarnedBadgeIds, residentId,
                    hasFiveWeekStreak, newlyAwarded);

                if (newlyAwarded.Any())
                    db.SaveChanges();

                return newlyAwarded;
            }

            private void TryAward(List<Badge> allBadges, string badgeName, List<int> alreadyEarnedBadgeIds,
                string residentId, bool criteriaMet, List<Badge> newlyAwarded)
            {
                if (!criteriaMet) return;

                var badge = allBadges.FirstOrDefault(b => b.Name == badgeName);
                if (badge == null) return;
                if (alreadyEarnedBadgeIds.Contains(badge.BadgeId)) return;

                db.UserBadges.Add(new UserBadge
                {
                    UserId = residentId,
                    BadgeId = badge.BadgeId
                });

                newlyAwarded.Add(badge);
            }

            private bool CheckFiveWeekStreak(List<RecyclingSubmission> verifiedSubmissions)
            {
                var today = DateTime.Now.Date;

                for (int weekOffset = 0; weekOffset < 5; weekOffset++)
                {
                    var weekStart = today.AddDays(-7 * (weekOffset + 1));
                    var weekEnd = today.AddDays(-7 * weekOffset);

                    bool hasSubmissionThisWeek = verifiedSubmissions
                        .Any(s => s.ProcessedAt.HasValue
                            && s.ProcessedAt.Value.Date >= weekStart
                            && s.ProcessedAt.Value.Date < weekEnd);

                    if (!hasSubmissionThisWeek)
                        return false;
                }

                return true;
            }
        public List<BadgeCabinetViewModel> GetBadgeCabinet(string residentId)
        {
            var verifiedSubmissions = db.RecyclingSubmissions
                .Where(s => s.ResidentId == residentId && s.Status == SubmissionStatus.Verified)
                .ToList();

            var earnedBadges = db.UserBadges
                .Where(ub => ub.UserId == residentId)
                .ToDictionary(ub => ub.BadgeId, ub => ub.EarnedAt);

            var allBadges = db.Badges.OrderBy(b => b.BadgeId).ToList();

            var totalWeight = verifiedSubmissions.Sum(s => s.WeightKg);
            var totalSubmissions = verifiedSubmissions.Count;
            var hasEWaste = verifiedSubmissions.Any(s => s.MaterialType != null && s.MaterialType.Name == "E-Waste");
            var streakWeeks = CalculateCurrentStreakWeeksForCabinet(verifiedSubmissions);

            var topResidentId = db.Users
                .Where(u => u.Role == "Resident" && u.IsActive)
                .OrderByDescending(u => u.PointsBalance)
                .Select(u => u.Id)
                .FirstOrDefault();

            var cabinet = new List<BadgeCabinetViewModel>();

            foreach (var badge in allBadges)
            {
                var isEarned = earnedBadges.ContainsKey(badge.BadgeId);

                var vm = new BadgeCabinetViewModel
                {
                    BadgeId = badge.BadgeId,
                    Name = badge.Name,
                    Description = badge.Description,
                    IconClass = badge.IconClass,
                    IsEarned = isEarned,
                    EarnedAt = isEarned ? earnedBadges[badge.BadgeId] : (DateTime?)null
                };

                switch (badge.Name)
                {
                    case "First Drop":
                        vm.ProgressCurrent = Math.Min(totalSubmissions, 1);
                        vm.ProgressTarget = 1;
                        vm.ProgressLabel = isEarned ? "Completed" : totalSubmissions + "/1 submissions";
                        break;

                    case "100kg Club":
                        vm.ProgressCurrent = (double)Math.Min(totalWeight, 100);
                        vm.ProgressTarget = 100;
                        vm.ProgressLabel = isEarned ? "Completed" : totalWeight.ToString("F1") + "/100 kg";
                        break;

                    case "E-Waste Hero":
                        vm.ProgressCurrent = hasEWaste ? 1 : 0;
                        vm.ProgressTarget = 1;
                        vm.ProgressLabel = isEarned ? "Completed" : "Log 1 E-Waste submission";
                        break;

                    case "Points Champion":
                        vm.ProgressCurrent = (topResidentId == residentId) ? 1 : 0;
                        vm.ProgressTarget = 1;
                        vm.ProgressLabel = isEarned ? "Completed" : "Reach #1 on the leaderboard";
                        break;

                    case "5-Week Streak":
                        vm.ProgressCurrent = streakWeeks;
                        vm.ProgressTarget = 5;
                        vm.ProgressLabel = isEarned ? "Completed" : streakWeeks + "/5 weeks";
                        break;

                    default:
                        vm.ProgressCurrent = 0;
                        vm.ProgressTarget = 1;
                        vm.ProgressLabel = "";
                        break;
                }

                vm.ProgressPercent = vm.ProgressTarget > 0
                    ? (int)Math.Min(100, (vm.ProgressCurrent / vm.ProgressTarget) * 100)
                    : 0;

                cabinet.Add(vm);
            }

            return cabinet;
        }

        private int CalculateCurrentStreakWeeksForCabinet(List<RecyclingSubmission> verifiedSubmissions)
        {
            var today = DateTime.Now.Date;
            int streak = 0;

            for (int weekOffset = 0; weekOffset < 5; weekOffset++)
            {
                var weekStart = today.AddDays(-7 * (weekOffset + 1));
                var weekEnd = today.AddDays(-7 * weekOffset);

                bool hasSubmissionThisWeek = verifiedSubmissions
                    .Any(s => s.ProcessedAt.HasValue
                        && s.ProcessedAt.Value.Date >= weekStart
                        && s.ProcessedAt.Value.Date < weekEnd);

                if (!hasSubmissionThisWeek) break;
                streak++;
            }

            return streak;
        }
    }
    }

