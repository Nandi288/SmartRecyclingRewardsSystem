using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public class Reward
    {
        public int RewardId { get; set; }

        [Required]
        [StringLength(150)]
        public string Name { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        [Required]
        [Display(Name = "Points Cost")]
        public int PointsCost { get; set; }

        public bool IsActive { get; set; } = true;

        [StringLength(300)]
        public string ImageUrl { get; set; }
    }
}