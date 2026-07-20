using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Services;
using SmartRecyclingRewardsSystem.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize]
    public class LeaderBoardController : Controller
    {
        
        private readonly LeaderboardService leaderboardService;

        public LeaderBoardController()
        {
            leaderboardService = new LeaderboardService();
        }

        public ActionResult Index()
        {
            var currentUserId = User.Identity.GetUserId();

            var viewModel = new LeaderboardPageViewModel
            {
                MonthlyRankings = leaderboardService.GetMonthlyLeaderboard(currentUserId),
                AllTimeRankings = leaderboardService.GetAllTimeLeaderboard(currentUserId)
            };

            return View(viewModel);
        }
    }

}
