using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartRecyclingRewardsSystem.ViewModels
{
    public class LeaderboardPageViewModel
    {
        public List<CommunityLeaderboardViewModel> MonthlyRankings {  get; set; }
        public List<CommunityLeaderboardViewModel> AllTimeRankings { get; set; }
    }
}