using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace SmartRecyclingRewardsSystem.Services
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly SmsService _sms;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
            _sms = new SmsService();
        }

        public async Task NotifyVerifiedAsync(ApplicationUser resident, RecyclingSubmission submission)
        {
            var message = string.Format(
                "Great news! Your recycling submission of {0} kg of {1} at {2} has been VERIFIED. You earned {3} points.",
                submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name, submission.PointsAwarded);

            await CreateAndSendAsync(resident, NotificationType.SubmissionVerified,
                "Submission Verified ✓", message, submission.RecyclingSubmissionId);
        }

        public async Task NotifyRejectedAsync(ApplicationUser resident, RecyclingSubmission submission)
        {
            var message = string.Format(
                "Your recycling submission of {0} kg of {1} at {2} has been REJECTED. Reason: {3}.",
                submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name,
                submission.RejectionReason ?? "No reason provided");

            await CreateAndSendAsync(resident, NotificationType.SubmissionRejected,
                "Submission Rejected", message, submission.RecyclingSubmissionId);
        }

        private async Task CreateAndSendAsync(ApplicationUser user, NotificationType type,
            string title, string message, int? submissionId = null)
        {
            bool emailSent = false;
            bool smsSent = false;

            if (user.ReceiveEmailNotifications && !string.IsNullOrWhiteSpace(user.Email))
                emailSent = await SendEmailAsync(user.Email, title, message);

            if (user.ReceiveSmsNotifications && !string.IsNullOrWhiteSpace(user.PhoneNumber))
                smsSent = await _sms.SendSmsAsync(user.PhoneNumber, title + ": " + message);

            var notification = new Notification
            {
                UserId = user.Id,
                NotificationType = type,
                Title = title,
                Message = message,
                IsRead = false,
                EmailSent = emailSent,
                SmsSent = smsSent,
                RecyclingSubmissionId = submissionId,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpHost = WebConfigurationManager.AppSettings["SmtpHost"] ?? "smtp.gmail.com";
                var smtpPort = int.Parse(WebConfigurationManager.AppSettings["SmtpPort"] ?? "587");
                var smtpFrom = WebConfigurationManager.AppSettings["SmtpFromEmail"] ?? "";
                var smtpUser = WebConfigurationManager.AppSettings["SmtpUsername"] ?? "";
                var smtpPassword = WebConfigurationManager.AppSettings["SmtpPassword"] ?? "";

                if (string.IsNullOrWhiteSpace(smtpFrom)) return false;

                using (var client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpUser, smtpPassword);

                    var mail = new MailMessage
                    {
                        From = new MailAddress(smtpFrom, "EcoRewards SA"),
                        Subject = subject,
                        Body = body + "\n\nEcoRewards SA — eThekwini, KwaZulu-Natal",
                        IsBodyHtml = false
                    };
                    mail.To.Add(toEmail);

                    await client.SendMailAsync(mail);
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
