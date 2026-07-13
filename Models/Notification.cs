using System;
using System.ComponentModel.DataAnnotations;

namespace SmartRecyclingRewardsSystem.Models
{
    // Every kind of notification/email the system can send.
    // The 3 new values at the bottom were added to support:
    //  - AccountCreated            → welcome email when a Resident/Officer registers
    //  - SubmissionReceived        → confirmation email when a Resident logs recycling
    //  - PendingVerificationAlert  → alert email to the Officer who needs to verify it
    public enum NotificationType
    {
        SubmissionVerified = 0,
        SubmissionRejected = 1,
        GeneralAnnouncement = 2,
        RewardRedeemed = 3,
        AccountCreated = 4,
        SubmissionReceived = 5,
        PendingVerificationAlert = 6
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