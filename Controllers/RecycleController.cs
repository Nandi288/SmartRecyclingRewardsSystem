using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.Services;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class RecycleController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly NotificationService _notificationService;

        public RecycleController()
        {
            _notificationService = new NotificationService(_db);
        }

        // GET: /Recycle/Log
        public ActionResult Log()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            if (user == null || user.Role != "Resident")
            {
                TempData["Error"] = "Only Residents can log recycling.";
                return RedirectToAction("Index", "Dashboard");
            }

            ViewBag.MaterialTypeId = new SelectList(
                _db.MaterialTypes.Where(m => m.IsActive).OrderBy(m => m.Name),
                "MaterialTypeId", "Name"
            );

            ViewBag.DropOffPointId = new SelectList(
                _db.DropOffPoints.Where(d => d.IsActive).OrderBy(d => d.Name),
                "DropOffPointId", "Name"
            );

            return View();
        }

        // POST: /Recycle/Log
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Log(RecyclingSubmission model)
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            if (user == null || user.Role != "Resident")
            {
                TempData["Error"] = "Only Residents can log recycling.";
                return RedirectToAction("Index", "Dashboard");
            }

            // ResidentId isn't posted by the form (it's set server-side below),
            // so it must be removed from ModelState before validation, otherwise
            // the [Required] attribute on ResidentId always fails.
            ModelState.Remove("ResidentId");

            if (ModelState.IsValid)
            {
                var material = _db.MaterialTypes.Find(model.MaterialTypeId);
                if (material == null)
                {
                    ModelState.AddModelError("MaterialTypeId", "Invalid material.");
                    PopulateDropdowns();
                    return View(model);
                }

                var dropOffPoint = _db.DropOffPoints.Find(model.DropOffPointId);
                if (dropOffPoint == null)
                {
                    ModelState.AddModelError("DropOffPointId", "Invalid drop-off point.");
                    PopulateDropdowns();
                    return View(model);
                }

                var submission = new RecyclingSubmission
                {
                    ResidentId = userId,
                    MaterialTypeId = model.MaterialTypeId,
                    DropOffPointId = model.DropOffPointId,
                    WeightKg = model.WeightKg,
                    Notes = model.Notes,
                    SubmissionDate = DateTime.Now,
                    Status = SubmissionStatus.Pending,
                    PointsAwarded = 0,
                    CO2SavedKg = 0
                };

                _db.RecyclingSubmissions.Add(submission);
                await _db.SaveChangesAsync();

                // Attach the already-loaded related entities so the notification
                // service can read their names without an extra database round-trip
                submission.MaterialType = material;
                submission.DropOffPoint = dropOffPoint;

                // Notify the resident: submission received, pending verification
                await _notificationService.NotifySubmissionReceivedAsync(user, submission);

                // Notify the collection officer assigned to this drop-off point,
                // if one has been assigned, that a new submission needs review
                if (!string.IsNullOrEmpty(dropOffPoint.AssignedOfficerId))
                {
                    var officer = await _db.Users.FirstOrDefaultAsync(u => u.Id == dropOffPoint.AssignedOfficerId);
                    if (officer != null)
                    {
                        await _notificationService.NotifyOfficerPendingSubmissionAsync(officer, submission, user);
                    }
                }

                TempData["Success"] = $"Your recycling submission of {submission.WeightKg} kg of {material.Name} has been logged successfully!";
                return RedirectToAction("MySubmissions");
            }

            PopulateDropdowns();
            return View(model);
        }

        // GET: /Recycle/MySubmissions
        public ActionResult MySubmissions()
        {
            var userId = User.Identity.GetUserId();
            var user = _db.Users.Find(userId);

            if (user == null || user.Role != "Resident")
            {
                TempData["Error"] = "Access denied.";
                return RedirectToAction("Index", "Dashboard");
            }

            var submissions = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == userId)
                .Include(s => s.MaterialType)
                .Include(s => s.DropOffPoint)
                .OrderByDescending(s => s.SubmissionDate)
                .ToList();

            return View(submissions);
        }

        private void PopulateDropdowns()
        {
            ViewBag.MaterialTypeId = new SelectList(
                _db.MaterialTypes.Where(m => m.IsActive).OrderBy(m => m.Name),
                "MaterialTypeId", "Name"
            );

            ViewBag.DropOffPointId = new SelectList(
                _db.DropOffPoints.Where(d => d.IsActive).OrderBy(d => d.Name),
                "DropOffPointId", "Name"
            );
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}