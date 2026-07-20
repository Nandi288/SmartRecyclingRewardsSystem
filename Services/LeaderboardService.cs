using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartRecyclingRewardsSystem.Services
{
    public class LeaderboardService
    {

        private readonly ApplicationDbContext db;

        public LeaderboardService()
        {
            db = new ApplicationDbContext();
        }

        public List<CommunityLeaderboardViewModel> GetAllTimeLeaderboard(string currentUserId)
        {
            var users = db.Users
                .Where(u => u.Role == "Resident" && u.IsActive)
                .ToList();

            var results = users
                .Select(u => new CommunityLeaderboardViewModel
                {
                    FirstName = u.FirstName,
                    LastName = u.LastName,
                    TotalPoints = u.PointsBalance,
                    TotalRecycledkg = u.RecyclingSubmissions
                        .Where(s => s.Status == SubmissionStatus.Verified)
                        .Sum(s => (decimal?)s.WeightKg) ?? 0,
                    IsCurrentUser = u.Id == currentUserId
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToList();

            AssignRanks(results);
            return results;
        }

        public List<CommunityLeaderboardViewModel> GetMonthlyLeaderboard(string currentUserId)
        {
            var now = DateTime.Now;

            var results = db.RecyclingSubmissions
                .Where(s => s.Status == SubmissionStatus.Verified
                    && s.ProcessedAt.HasValue
                    && s.ProcessedAt.Value.Month == now.Month
                    && s.ProcessedAt.Value.Year == now.Year)
                .GroupBy(s => s.Resident)
                .Select(g => new CommunityLeaderboardViewModel
                {
                    FirstName = g.Key.FirstName,
                    LastName = g.Key.LastName,
                    TotalPoints = g.Sum(s => s.PointsAwarded),
                    TotalRecycledkg = g.Sum(s => s.WeightKg),
                    IsCurrentUser = g.Key.Id == currentUserId
                })
                .OrderByDescending(x => x.TotalPoints)
                .ToList();

            AssignRanks(results);
            return results;
        }

        private void AssignRanks(List<CommunityLeaderboardViewModel> list)
        {
            for (int i = 0; i < list.Count; i++)
                list[i].Rank = i + 1;
        }
    }

}
