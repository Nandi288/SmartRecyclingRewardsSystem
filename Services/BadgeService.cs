using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;


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
        }
    }

