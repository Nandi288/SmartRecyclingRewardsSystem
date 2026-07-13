using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    // Only Admins are allowed to access any action in this controller
    [Authorize(Roles = "Admin")]
    public class UserManagementController : Controller
    {
        // Database context used to read/update users and related tables
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // UserManager handles Identity operations like deleting a user
        // (it also cleans up related Identity tables such as roles/logins)
        private ApplicationUserManager _userManager;
        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        // ============================================================
        // GET: /UserManagement/Index
        // Displays a table of every user in the system
        // ============================================================
        public ActionResult Index()
        {
            var users = _db.Users
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToList();

            return View("ListUsers_Index", users);
        }

        // ============================================================
        // POST: /UserManagement/Deactivate/5
        // Deactivates a user so they can no longer log in
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Deactivate(string id)
        {
            var currentUserId = User.Identity.GetUserId();
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Index");
            }

            // IMPORTANT: fetch the user through _db (the SAME context we call
            // SaveChangesAsync on below), NOT through UserManager.FindByIdAsync.
            // UserManager uses its own separate DbContext instance internally,
            // so a change made to a user fetched from UserManager would never
            // actually get written to the database when we save through _db.
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            user.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{user.FullName} has been deactivated and can no longer log in.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // POST: /UserManagement/Activate/5
        // Re-activates a previously deactivated user
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Activate(string id)
        {
            // Same fix as Deactivate: load through _db, not UserManager
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            user.IsActive = true;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{user.FullName} has been re-activated and can log in again.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // GET: /UserManagement/Delete/5
        // Shows confirmation page for deleting a user
        // ============================================================
        public async Task<ActionResult> Delete(string id)
        {
            var currentUserId = User.Identity.GetUserId();
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Let the confirmation page know whether this user has related
            // records, so it can warn the admin that this data will also
            // be permanently removed.
            ViewBag.HasHistory =
                _db.RecyclingSubmissions.Any(s => s.ResidentId == id || s.VerifiedByOfficerId == id) ||
                _db.PointTransactions.Any(t => t.UserId == id) ||
                _db.Notifications.Any(n => n.UserId == id) ||
                _db.CollectionEventRegistrations.Any(r => r.ResidentId == id) ||
                _db.DropOffPoints.Any(d => d.AssignedOfficerId == id) ||
                _db.UserBadges.Any(b => b.UserId == id);

            return View("Delete", user);
        }

        // ============================================================
        // POST: /UserManagement/Delete/5
        // Permanently deletes a user AND every record that links back to
        // them (submissions, point transactions, notifications, event
        // registrations, badges), so the delete always succeeds instead
        // of being blocked by the database's foreign keys.
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(string id)
        {
            var currentUserId = User.Identity.GetUserId();
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot delete your own account.";
                return RedirectToAction("Index");
            }

            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            var userName = user.FullName;

            // Submissions this user made as a Resident
            var ownSubmissionIds = _db.RecyclingSubmissions
                .Where(s => s.ResidentId == id)
                .Select(s => s.RecyclingSubmissionId)
                .ToList();

            // 1) Remove any notifications that point at those submissions
            //    (they may belong to an officer, not this user)
            var linkedNotifications = _db.Notifications
                .Where(n => ownSubmissionIds.Contains(n.RecyclingSubmissionId ?? -1));
            _db.Notifications.RemoveRange(linkedNotifications);

            // 2) Remove any point transactions tied to those submissions
            var linkedTransactions = _db.PointTransactions
                .Where(t => ownSubmissionIds.Contains(t.RecyclingSubmissionId ?? -1));
            _db.PointTransactions.RemoveRange(linkedTransactions);

            // 3) Now it's safe to remove the resident's own submissions
            var ownSubmissions = _db.RecyclingSubmissions.Where(s => s.ResidentId == id);
            _db.RecyclingSubmissions.RemoveRange(ownSubmissions);

            // 4) If this user is a Collection Officer who verified other
            //    residents' submissions, unlink them instead of deleting
            //    (those submissions still belong to other residents)
            var verifiedByThisUser = _db.RecyclingSubmissions.Where(s => s.VerifiedByOfficerId == id);
            foreach (var s in verifiedByThisUser)
            {
                s.VerifiedByOfficerId = null;
            }

            // 5) Remove this user's own remaining point transactions
            //    (e.g. reward redemptions not tied to a submission)
            var remainingTransactions = _db.PointTransactions.Where(t => t.UserId == id);
            _db.PointTransactions.RemoveRange(remainingTransactions);

            // 6) Remove this user's own remaining notifications
            var remainingNotifications = _db.Notifications.Where(n => n.UserId == id);
            _db.Notifications.RemoveRange(remainingNotifications);

            // 7) Remove event registrations made by this user
            var registrations = _db.CollectionEventRegistrations.Where(r => r.ResidentId == id);
            _db.CollectionEventRegistrations.RemoveRange(registrations);

            // 8) Remove badges earned by this user
            var badges = _db.UserBadges.Where(b => b.UserId == id);
            _db.UserBadges.RemoveRange(badges);

            // 9) If this user is an Officer assigned to any drop-off points,
            //    unassign them instead of deleting the drop-off point
            var assignedPoints = _db.DropOffPoints.Where(d => d.AssignedOfficerId == id);
            foreach (var d in assignedPoints)
            {
                d.AssignedOfficerId = null;
            }

            // Save all of the cleanup above before deleting the user itself
            await _db.SaveChangesAsync();

            // Finally, delete the user account (UserManager also cleans up
            // Identity tables like roles/logins for this user)
            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{userName} and all related records have been permanently deleted.";
            }
            else
            {
                TempData["Error"] = "Could not delete user: " + string.Join(" ", result.Errors);
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}