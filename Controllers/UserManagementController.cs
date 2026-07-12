using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
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

        // UserManager handles Identity operations like finding/deleting a user
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
            // Get the ID of the admin who is currently logged in
            var currentUserId = User.Identity.GetUserId();

            // Safety check: an admin cannot deactivate their own account
            if (id == currentUserId)
            {
                TempData["Error"] = "You cannot deactivate your own account.";
                return RedirectToAction("Index");
            }

            // FindByIdAsync is the correct async lookup method for Identity users
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            // Flip the flag off — AccountController checks this at login
            user.IsActive = false;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{user.FullName} has been deactivated.";
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
            var user = await UserManager.FindByIdAsync(id);
            if (user == null)
            {
                TempData["Error"] = "User not found.";
                return RedirectToAction("Index");
            }

            user.IsActive = true;
            await _db.SaveChangesAsync();

            TempData["Success"] = $"{user.FullName} has been re-activated.";
            return RedirectToAction("Index");
        }

        // ============================================================
        // GET: /UserManagement/Delete/5
        // Shows a confirmation page before deleting a user
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

            return View("Delete", user);
        }

        // ============================================================
        // POST: /UserManagement/Delete/5
        // Permanently deletes the user — unless they have existing
        // recycling history, in which case we deactivate them instead
        // so we don't break foreign-key links to their old records
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

            // Check every table that links back to this user
            bool hasHistory =
                _db.RecyclingSubmissions.Any(s => s.ResidentId == id || s.VerifiedByOfficerId == id) ||
                _db.PointTransactions.Any(t => t.UserId == id) ||
                _db.Notifications.Any(n => n.UserId == id) ||
                _db.CollectionEventRegistrations.Any(r => r.ResidentId == id) ||
                _db.DropOffPoints.Any(d => d.AssignedOfficerId == id);

            if (hasHistory)
            {
                // Deleting would break linked records, so deactivate instead
                user.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Info"] = $"{user.FullName} has existing recycling activity, so the account was deactivated instead of deleted.";
                return RedirectToAction("Index");
            }

            // No history found — safe to permanently delete
            var result = await UserManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = $"{user.FullName} has been permanently deleted.";
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