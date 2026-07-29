using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
        public class BadgeController : Controller
        {
            private readonly BadgeService _badgeService = new BadgeService();

         
            public ActionResult Cabinet()
            {
                var residentId = User.Identity.GetUserId();
                var cabinet = _badgeService.GetBadgeCabinet(residentId);
                return View(cabinet);
            }
        }
    
}
