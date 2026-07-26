using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;

namespace SmartRecyclingRewardsSystem.ViewModels
{
    public class NotificationViewModel
    {
        public int NotificationId { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public NotificationType NotificationType { get; set; }
        public bool IsRead { get; set; }
        public bool EmailSent { get; set; }
        public bool SmsSent { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? RecyclingSubmissionId { get; set; }

        // UI helper properties
        public string TimeAgo => GetTimeAgo(CreatedAt);
        public string IconClass => GetIconForType(NotificationType);
        public string ColorClass => GetColorForType(NotificationType);
        public string BadgeClass => GetBadgeForType(NotificationType);

        private string GetTimeAgo(DateTime date)
        {
            var diff = DateTime.Now - date;
            if (diff.TotalSeconds < 60) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
            if (diff.TotalDays < 30) return $"{(int)(diff.TotalDays / 7)}w ago";
            if (diff.TotalDays < 365) return $"{date.ToString("dd MMM")}";
            return date.ToString("dd MMM yyyy");
        }

        private string GetIconForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.AccountCreated:
                    return "fa-user-plus";
                case NotificationType.SubmissionReceived:
                    return "fa-clock";
                case NotificationType.SubmissionVerified:
                    return "fa-check-circle";
                case NotificationType.SubmissionRejected:
                    return "fa-times-circle";
                case NotificationType.PendingVerificationAlert:
                    return "fa-bell";
                case NotificationType.GeneralAnnouncement:
                    return "fa-bullhorn";
                case NotificationType.RewardRedeemed:
                    return "fa-gift";
                default:
                    return "fa-bell";
            }
        }

        private string GetColorForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.AccountCreated:
                    return "text-success";
                case NotificationType.SubmissionReceived:
                    return "text-warning";
                case NotificationType.SubmissionVerified:
                    return "text-success";
                case NotificationType.SubmissionRejected:
                    return "text-danger";
                case NotificationType.PendingVerificationAlert:
                    return "text-info";
                case NotificationType.GeneralAnnouncement:
                    return "text-primary";
                case NotificationType.RewardRedeemed:
                    return "text-warning";
                default:
                    return "text-muted";
            }
        }

        private string GetBadgeForType(NotificationType type)
        {
            switch (type)
            {
                case NotificationType.AccountCreated:
                    return "bg-success";
                case NotificationType.SubmissionReceived:
                    return "bg-warning text-dark";
                case NotificationType.SubmissionVerified:
                    return "bg-success";
                case NotificationType.SubmissionRejected:
                    return "bg-danger";
                case NotificationType.PendingVerificationAlert:
                    return "bg-info";
                case NotificationType.GeneralAnnouncement:
                    return "bg-primary";
                case NotificationType.RewardRedeemed:
                    return "bg-warning text-dark";
                default:
                    return "bg-secondary";
            }
        }
    }
}