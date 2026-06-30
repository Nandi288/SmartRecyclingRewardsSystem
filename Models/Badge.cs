using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class Badge
    {
        public int BadgeId { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [StringLength(100)]
        public string Criteria { get; set; }

        [StringLength(500)]
        public string IconClass { get; set; }

        public virtual ICollection<UserBadge> UserBadges { get; set; }
    }

    public class UserBadge
    {
        public int UserBadgeId { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        [Required]
        public int BadgeId { get; set; }
        public virtual Badge Badge { get; set; }

        public DateTime EarnedAt { get; set; }

        public UserBadge()
        {
            EarnedAt = DateTime.Now;
        }
    }
}
