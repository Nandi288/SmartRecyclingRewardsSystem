//using System;
//using System.ComponentModel.DataAnnotations;

//namespace SmartRecyclingRewardsSystem.Models
//{
//    public class RewardRedemption
//    {
//        public int RewardRedemptionId { get; set; }

//        [Required]
//        public string UserId { get; set; }
//        public virtual ApplicationUser User { get; set; }

//        [Required]
//        public int RewardId { get; set; }
//        public virtual Reward Reward { get; set; }

//        [Required]
//        public int PointsSpent { get; set; }

//        public DateTime RedemptionDate { get; set; }

//        public RewardRedemption()
//        {
//            RedemptionDate = DateTime.Now;
//        }
//    }
//}

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

        public DateTime RedemptionDate { get; set; } = DateTime.Now;

        // ===== COUPON & PAYMENT FIELDS =====

        [StringLength(20)]
        public string CouponCode { get; set; }

        [StringLength(200)]
        public string CouponDetails { get; set; }

        public DateTime? CouponExpiryDate { get; set; }

        [StringLength(50)]
        public string Status { get; set; } = "Pending";

        [StringLength(50)]
        public string TransactionId { get; set; }

        [StringLength(20)]
        public string PaymentMethod { get; set; }

        public DateTime? CompletionDate { get; set; }

        public DateTime? UsedDate { get; set; }
    }
}