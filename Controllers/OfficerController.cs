using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "CollectionOfficer")]
    public class OfficerController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly NotificationService _notificationService;
        private readonly BadgeService _badgeService;

        public OfficerController()
        {
            _notificationService = new NotificationService(_db);
            _badgeService = new BadgeService();
        }

        // ============================================================
        // GET: /Officer/Pending
        // View all pending submissions at assigned drop-off point
        // ============================================================
        public ActionResult Pending()
        {
            var officerId = User.Identity.GetUserId();
            var assignedIds = _db.DropOffPoints
                .Where(d => d.AssignedOfficerId == officerId && d.IsActive)
                .Select(d => d.DropOffPointId)
                .ToList();

            var pending = _db.RecyclingSubmissions
                .Where(s => assignedIds.Contains(s.DropOffPointId)
                         && s.Status == SubmissionStatus.Pending)
                .Include(s => s.Resident)
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .OrderByDescending(s => s.SubmissionDate)
                .ToList();

            return View(pending);
        }

        // ============================================================
        // GET: /Officer/AllSubmissions
        // View ALL submissions at assigned drop-off points
        // ============================================================
        public ActionResult AllSubmissions(string status = "all")
        {
            var officerId = User.Identity.GetUserId();
            var assignedIds = _db.DropOffPoints
                .Where(d => d.AssignedOfficerId == officerId && d.IsActive)
                .Select(d => d.DropOffPointId)
                .ToList();

            var query = _db.RecyclingSubmissions
                .Where(s => assignedIds.Contains(s.DropOffPointId))
                .Include(s => s.Resident)
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .AsQueryable();

            if (status == "pending")
                query = query.Where(s => s.Status == SubmissionStatus.Pending);
            else if (status == "verified")
                query = query.Where(s => s.Status == SubmissionStatus.Verified);
            else if (status == "rejected")
                query = query.Where(s => s.Status == SubmissionStatus.Rejected);

            var submissions = query
                .OrderByDescending(s => s.SubmissionDate)
                .ToList();

            ViewBag.CurrentStatus = status;
            ViewBag.PendingCount = _db.RecyclingSubmissions
                .Count(s => assignedIds.Contains(s.DropOffPointId) && s.Status == SubmissionStatus.Pending);

            return View(submissions);
        }

        // ============================================================
        // GET: /Officer/Verify/5
        // Show the verify/reject form for a submission
        // ============================================================
        public ActionResult Verify(int id)
        {
            var officerId = User.Identity.GetUserId();
            var submission = _db.RecyclingSubmissions
                .Include(s => s.Resident)
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .FirstOrDefault(s => s.RecyclingSubmissionId == id);

            if (submission == null)
            {
                TempData["Error"] = "Submission not found.";
                return RedirectToAction("Pending");
            }

            if (submission.Status != SubmissionStatus.Pending)
            {
                TempData["Error"] = "This submission has already been processed.";
                return RedirectToAction("Pending");
            }

            return View(submission);
        }

        // ============================================================
        // POST: /Officer/ConfirmVerify/5
        // Verifies the submission and awards points
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmVerify(int id)
        {
            var officerId = User.Identity.GetUserId();
            var submission = await _db.RecyclingSubmissions
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .FirstOrDefaultAsync(s => s.RecyclingSubmissionId == id);

            if (submission == null || submission.Status != SubmissionStatus.Pending)
            {
                TempData["Error"] = "Submission not found or already processed.";
                return RedirectToAction("Pending");
            }

            // Calculate points and CO2
            var pointsAwarded = (int)Math.Floor(submission.WeightKg * submission.MaterialType.PointsPerKg);
            var co2Saved = submission.WeightKg * submission.MaterialType.CO2SavingPerKg;

            // Update submission
            submission.Status = SubmissionStatus.Verified;
            submission.VerifiedByOfficerId = officerId;
            submission.ProcessedAt = DateTime.Now;
            submission.PointsAwarded = pointsAwarded;
            submission.CO2SavedKg = co2Saved;

            // Update resident points balance
            var resident = _db.Users.Find(submission.ResidentId);
            resident.PointsBalance += pointsAwarded;

            // Create point transaction record
            var transaction = new PointTransaction
            {
                UserId = submission.ResidentId,
                TransactionType = TransactionType.Earned,
                Points = pointsAwarded,
                BalanceAfter = resident.PointsBalance,
                Description = string.Format("Verified: {0} kg of {1} at {2}",
                    submission.WeightKg, submission.MaterialType.Name, submission.DropOffPoint.Name),
                TransactionDate = DateTime.Now,
                RecyclingSubmissionId = submission.RecyclingSubmissionId
            };
            _db.PointTransactions.Add(transaction);

            // Create in-app notification for resident, and send them an email.
            // This also saves all the changes made above (submission status,
            // points balance, point transaction) in the same SaveChanges call.
            await _notificationService.NotifyVerifiedAsync(resident, submission);

            // Check badge criteria now that this submission is verified (UC-14)
            var newBadges = _badgeService.CheckAndAwardBadges(submission.ResidentId);
            foreach (var badge in newBadges)
            {
                await _notificationService.NotifyBadgeEarnedAsync(resident, badge);
            }

            TempData["Success"] = string.Format(
                "Submission verified! {0} earned {1} points.",
                resident.FullName, pointsAwarded);

            return RedirectToAction("Pending"); ;
        }

        // ============================================================
        // POST: /Officer/ConfirmReject/5
        // Rejects the submission with a reason
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ConfirmReject(int id, string rejectionReason)
        {
            if (string.IsNullOrWhiteSpace(rejectionReason))
            {
                TempData["Error"] = "Please enter a rejection reason.";
                return RedirectToAction("Verify", new { id });
            }

            var officerId = User.Identity.GetUserId();
            var submission = await _db.RecyclingSubmissions
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .FirstOrDefaultAsync(s => s.RecyclingSubmissionId == id);

            if (submission == null || submission.Status != SubmissionStatus.Pending)
            {
                TempData["Error"] = "Submission not found or already processed.";
                return RedirectToAction("Pending");
            }

            submission.Status = SubmissionStatus.Rejected;
            submission.VerifiedByOfficerId = officerId;
            submission.ProcessedAt = DateTime.Now;
            submission.RejectionReason = rejectionReason;
            submission.PointsAwarded = 0;

            // Notify resident: in-app notification + email, and save all
            // the changes made above (submission status, rejection reason)
            // in the same SaveChanges call
            var resident = _db.Users.Find(submission.ResidentId);
            await _notificationService.NotifyRejectedAsync(resident, submission);

            TempData["Success"] = string.Format(
                "Submission from {0} has been rejected.", resident.FullName);

            return RedirectToAction("Pending");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}