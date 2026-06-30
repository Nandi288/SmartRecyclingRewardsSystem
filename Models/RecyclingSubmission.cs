using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartRecyclingRewardsSystem.Models
{
    public enum SubmissionStatus
    {
        Pending = 0,
        Verified = 1,
        Rejected = 2
    }

    public class RecyclingSubmission
    {
        public int RecyclingSubmissionId { get; set; }

        [Required]
        public string ResidentId { get; set; }
        public virtual ApplicationUser Resident { get; set; }

        [Required]
        [Display(Name = "Material Type")]
        public int MaterialTypeId { get; set; }
        public virtual MaterialType MaterialType { get; set; }

        [Required]
        [Display(Name = "Drop-Off Point")]
        public int DropOffPointId { get; set; }
        public virtual DropOffPoint DropOffPoint { get; set; }

        [Required]
        [Display(Name = "Weight (kg)")]
        [Column(TypeName = "decimal")]
        [Range(0.01, 99999.99, ErrorMessage = "Weight must be between 0.01 and 99999.99 kg")]
        public decimal WeightKg { get; set; }

        [Display(Name = "Submission Date")]
        public DateTime SubmissionDate { get; set; }

        public SubmissionStatus Status { get; set; }

        public string VerifiedByOfficerId { get; set; }
        public virtual ApplicationUser VerifiedByOfficer { get; set; }

        [Display(Name = "Processed On")]
        public DateTime? ProcessedAt { get; set; }

        [StringLength(500)]
        [Display(Name = "Rejection Reason")]
        public string RejectionReason { get; set; }

        [Display(Name = "Points Awarded")]
        public int PointsAwarded { get; set; }

        [Column(TypeName = "decimal")]
        [Display(Name = "CO2 Saved (kg)")]
        public decimal CO2SavedKg { get; set; }

        [StringLength(1000)]
        public string Notes { get; set; }

        public RecyclingSubmission()
        {
            SubmissionDate = DateTime.Now;
            Status = SubmissionStatus.Pending;
            PointsAwarded = 0;
            CO2SavedKg = 0;
        }
    }
}
