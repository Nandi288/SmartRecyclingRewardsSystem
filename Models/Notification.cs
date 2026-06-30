using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    public enum NotificationType
    {
        SubmissionVerified = 0,
        SubmissionRejected = 1,
        GeneralAnnouncement = 2,
        RewardRedeemed = 3
    }

    public class Notification
    {
        public int NotificationId { get; set; }

        [Required]
        public string UserId { get; set; }
        public virtual ApplicationUser User { get; set; }

        public NotificationType NotificationType { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(1000)]
        public string Message { get; set; }

        public bool IsRead { get; set; }
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }

        public DateTime CreatedAt { get; set; }

        public int? RecyclingSubmissionId { get; set; }
        public virtual RecyclingSubmission RecyclingSubmission { get; set; }

        public Notification()
        {
            IsRead = false;
            EmailSent = false;
            SmsSent = false;
            CreatedAt = DateTime.Now;
        }
    }
}
