using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public enum TransactionType
    {
        Earned = 0,
        Redeemed = 1,
        Adjusted = 2
    }

    public class PointTransaction
    {
        public int PointTransactionId { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public TransactionType TransactionType { get; set; }

        [Required]
        public int Points { get; set; }

        [Display(Name = "Balance After")]
        public int BalanceAfter { get; set; }

        [StringLength(500)]
        public string Description { get; set; }

        public DateTime TransactionDate { get; set; }

        public int? RecyclingSubmissionId { get; set; }
        public virtual RecyclingSubmission RecyclingSubmission { get; set; }

        public PointTransaction()
        {
            TransactionDate = DateTime.Now;
        }
    }
}
