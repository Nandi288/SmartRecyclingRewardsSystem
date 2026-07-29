using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace SmartRecyclingRewardsSystem.ViewModels
{
        public class BadgeCabinetViewModel
        {
            public int BadgeId { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string IconClass { get; set; }
            public bool IsEarned { get; set; }
            public DateTime? EarnedAt { get; set; }

            public double ProgressCurrent { get; set; }
            public double ProgressTarget { get; set; }
            public int ProgressPercent { get; set; }
            public string ProgressLabel { get; set; }
        }
}