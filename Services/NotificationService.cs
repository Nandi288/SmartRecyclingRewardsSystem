using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;

namespace SmartRecyclingRewardsSystem.Services
{
    // Central place for creating in-app notifications AND sending the
    // matching email (and SMS, if enabled) for each event in the system.
    public class NotificationService
    {
        private readonly ApplicationDbContext _db;
        private readonly SmsService _sms;

        public NotificationService(ApplicationDbContext db)
        {
            _db = db;
            _sms = new SmsService();
        }

        // ============================================================
        // Sent to a Resident or Collection Officer right after their
        // account is created (called from AccountController.Register)
        // ============================================================
        public async Task NotifyAccountCreatedAsync(ApplicationUser user)
        {
            var roleLabel = user.Role == "CollectionOfficer" ? "Collection Officer" : user.Role;

            var plainMessage = string.Format(
                "Welcome to EcoRewards SA, {0}! Your {1} account has been created successfully.",
                user.FirstName, roleLabel);

            // Each row is a 2-item array: { "Label", "Value" }
            var details = new List<string[]>
            {
                new[] { "Name", user.FullName },
                new[] { "Email", user.Email },
                new[] { "Role", roleLabel },
                new[] { "Date Joined", user.DateJoined.ToString("dd MMM yyyy") }
            };

            var html = BuildEmailHtml(
                "Welcome to EcoRewards SA!",
                string.Format("Hi {0}, your account has been created successfully. Here are your account details:", user.FirstName),
                details,
                "Start Recycling", "/Dashboard");

            await CreateAndSendAsync(user, NotificationType.AccountCreated,
                "Welcome to EcoRewards SA — Account Created", plainMessage, html);
        }

        // ============================================================
        // Sent to a Resident right after they submit recycling for
        // verification (called from RecycleController.Log)
        // ============================================================
        public async Task NotifySubmissionReceivedAsync(ApplicationUser resident, RecyclingSubmission submission)
        {
            var plainMessage = string.Format(
                "Your submission of {0} kg of {1} at {2} is pending verification.",
                submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name);

            var details = new List<string[]>
            {
                new[] { "Material", submission.MaterialType.Name },
                new[] { "Weight", submission.WeightKg + " kg" },
                new[] { "Drop-Off Point", submission.DropOffPoint.Name },
                new[] { "Submitted", submission.SubmissionDate.ToString("dd MMM yyyy, HH:mm") },
                new[] { "Status", "Pending Verification" }
            };

            var html = BuildEmailHtml(
                "Submission Received",
                string.Format("Hi {0}, thanks for recycling! Your submission has been received and is now pending verification.", resident.FirstName),
                details,
                "View My Submissions", "/Recycle/MySubmissions");

            await CreateAndSendAsync(resident, NotificationType.SubmissionReceived,
                "Submission Received", plainMessage, html, submission.RecyclingSubmissionId);
        }

        // ============================================================
        // Sent to the Collection Officer assigned to the drop-off point
        // where a new submission is awaiting their verification
        // ============================================================
        public async Task NotifyOfficerPendingSubmissionAsync(ApplicationUser officer, RecyclingSubmission submission, ApplicationUser resident)
        {
            var plainMessage = string.Format(
                "A new submission at {0} is waiting for your verification.", submission.DropOffPoint.Name);

            var details = new List<string[]>
            {
                new[] { "Resident", resident.FullName },
                new[] { "Material", submission.MaterialType.Name },
                new[] { "Weight", submission.WeightKg + " kg" },
                new[] { "Drop-Off Point", submission.DropOffPoint.Name },
                new[] { "Submitted", submission.SubmissionDate.ToString("dd MMM yyyy, HH:mm") }
            };

            var html = BuildEmailHtml(
                "New Submission Awaiting Verification",
                string.Format("Hi {0}, a resident has just logged a new submission at your assigned drop-off point.", officer.FirstName),
                details,
                "Review Submission", "/Officer/Pending");

            await CreateAndSendAsync(officer, NotificationType.PendingVerificationAlert,
                "New Submission Awaiting Verification", plainMessage, html, submission.RecyclingSubmissionId);
        }

        // ============================================================
        // Sent to a Resident when their submission is verified and
        // points are awarded (called from OfficerController.ConfirmVerify)
        // ============================================================
        public async Task NotifyVerifiedAsync(ApplicationUser resident, RecyclingSubmission submission)
        {
            var plainMessage = string.Format(
                "Your submission of {0} kg of {1} at {2} has been VERIFIED. You earned {3} points.",
                submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name, submission.PointsAwarded);

            var details = new List<string[]>
            {
                new[] { "Material", submission.MaterialType.Name },
                new[] { "Weight", submission.WeightKg + " kg" },
                new[] { "Drop-Off Point", submission.DropOffPoint.Name },
                new[] { "Points Earned", "+" + submission.PointsAwarded + " pts" },
                new[] { "Status", "Verified" }
            };

            var html = BuildEmailHtml(
                "Submission Verified",
                string.Format("Great news, {0}! Your recycling submission has been verified.", resident.FirstName),
                details,
                "View My Points", "/Points");

            await CreateAndSendAsync(resident, NotificationType.SubmissionVerified,
                "Submission Verified", plainMessage, html, submission.RecyclingSubmissionId);
        }

        // ============================================================
        // Sent to a Resident when their submission is rejected
        // (called from OfficerController.ConfirmReject)
        // ============================================================
        public async Task NotifyRejectedAsync(ApplicationUser resident, RecyclingSubmission submission)
        {
            var reason = submission.RejectionReason ?? "No reason provided";

            var plainMessage = string.Format(
                "Your submission of {0} kg of {1} at {2} has been REJECTED. Reason: {3}.",
                submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name, reason);

            var details = new List<string[]>
            {
                new[] { "Material", submission.MaterialType.Name },
                new[] { "Weight", submission.WeightKg + " kg" },
                new[] { "Drop-Off Point", submission.DropOffPoint.Name },
                new[] { "Reason", reason },
                new[] { "Status", "Rejected" }
            };

            var html = BuildEmailHtml(
                "Submission Rejected",
                string.Format("Hi {0}, unfortunately your recycling submission could not be verified.", resident.FirstName),
                details,
                "Submit Again", "/Recycle/Log");

            await CreateAndSendAsync(resident, NotificationType.SubmissionRejected,
                "Submission Rejected", plainMessage, html, submission.RecyclingSubmissionId);
        }

        // ============================================================
        // Shared helper: creates the in-app Notification row, and sends
        // an email (and SMS, if the user opted in) for every event above.
        // "plainMessage" is stored in the database for the in-app bell
        // icon; "htmlBody" is the styled version actually emailed out.
        // ============================================================
        private async Task CreateAndSendAsync(ApplicationUser user, NotificationType type,
            string title, string plainMessage, string htmlBody, int? submissionId = null)
        {
            bool emailSent = false;
            bool smsSent = false;

            if (user.ReceiveEmailNotifications && !string.IsNullOrWhiteSpace(user.Email))
                emailSent = await SendEmailAsync(user.Email, title, htmlBody);

            if (user.ReceiveSmsNotifications && !string.IsNullOrWhiteSpace(user.PhoneNumber))
                smsSent = await _sms.SendSmsAsync(user.PhoneNumber, title + ": " + plainMessage);

            var notification = new Notification
            {
                UserId = user.Id,
                NotificationType = type,
                Title = title,
                Message = plainMessage,
                IsRead = false,
                EmailSent = emailSent,
                SmsSent = smsSent,
                RecyclingSubmissionId = submissionId,
                CreatedAt = DateTime.Now
            };

            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
        }

        // ============================================================
        // Sends the actual email via SMTP, using credentials read from
        // Web.config (appSettings: SmtpHost, SmtpPort, SmtpFromEmail,
        // SmtpUsername, SmtpPassword). Returns false silently if sending
        // fails, so it never crashes the calling controller action.
        // ============================================================
        private async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
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
                        Body = htmlBody,
                        IsBodyHtml = true
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

        // ============================================================
        // Builds a styled HTML email: green header banner, white content
        // card with a headline + intro line + a details table, an
        // optional call-to-action button, and a footer. Shared by every
        // Notify method above so all emails look consistent.
        // Each item in "details" is a 2-item array: { label, value }.
        // ============================================================
        private string BuildEmailHtml(string headline, string introLine,
            List<string[]> details, string buttonText = null, string buttonPath = null)
        {
            var siteUrl = WebConfigurationManager.AppSettings["SiteBaseUrl"] ?? "http://localhost";

            var rowsHtml = new StringBuilder();
            foreach (var row in details)
            {
                var label = row[0];
                var value = row[1];
                rowsHtml.Append(
                    "<tr>" +
                    "<td style=\"padding:10px 16px;border-bottom:1px solid #eef2ee;color:#6b7c6e;font-size:13px;\">" + label + "</td>" +
                    "<td style=\"padding:10px 16px;border-bottom:1px solid #eef2ee;color:#1f2a24;font-size:13px;font-weight:600;text-align:right;\">" + value + "</td>" +
                    "</tr>");
            }

            var buttonHtml = "";
            if (!string.IsNullOrEmpty(buttonText) && !string.IsNullOrEmpty(buttonPath))
            {
                buttonHtml =
                    "<div style=\"text-align:center;margin-top:28px;\">" +
                    "<a href=\"" + siteUrl + buttonPath + "\" " +
                    "style=\"background:#2d6a4f;color:#ffffff;text-decoration:none;padding:13px 32px;border-radius:8px;" +
                    "font-family:Arial,sans-serif;font-size:14px;font-weight:700;display:inline-block;\">" + buttonText + "</a>" +
                    "</div>";
            }

            return
                "<!DOCTYPE html><html><body style=\"margin:0;padding:0;background:#f4f7f4;font-family:Arial,sans-serif;\">" +
                "<div style=\"max-width:560px;margin:0 auto;padding:24px 16px;\">" +

                    // Header banner
                    "<div style=\"background:#2d6a4f;border-radius:12px 12px 0 0;padding:24px 28px;text-align:center;\">" +
                    "<span style=\"color:#ffffff;font-size:20px;font-weight:800;letter-spacing:0.3px;\">EcoRewards SA</span>" +
                    "</div>" +

                    // Content card
                    "<div style=\"background:#ffffff;border:1px solid #e5ece7;border-top:none;border-radius:0 0 12px 12px;padding:28px;\">" +
                    "<h2 style=\"margin:0 0 12px;color:#1f2a24;font-size:20px;\">" + headline + "</h2>" +
                    "<p style=\"margin:0 0 20px;color:#4b564e;font-size:14px;line-height:1.5;\">" + introLine + "</p>" +

                    "<table style=\"width:100%;border-collapse:collapse;background:#f8fdf9;border-radius:8px;\">" +
                    rowsHtml +
                    "</table>" +

                    buttonHtml +
                    "</div>" +

                    // Footer
                    "<p style=\"text-align:center;color:#9aa79d;font-size:11px;margin-top:20px;\">" +
                    "EcoRewards SA &mdash; eThekwini, KwaZulu-Natal<br/>You're receiving this because email notifications are enabled on your account." +
                    "</p>" +
                "</div>" +
                "</body></html>";
        }
    }
}