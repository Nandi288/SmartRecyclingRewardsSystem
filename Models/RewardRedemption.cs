using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class RewardRedemption
    {
        public int RewardRedemptionId { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int RewardId { get; set; }
        public virtual Reward Reward { get; set; }

        [Required]
        public int PointsSpent { get; set; }

        public DateTime RedemptionDate { get; set; }

        public RewardRedemption()
        {
            RedemptionDate = DateTime.Now;
        }
    }
}