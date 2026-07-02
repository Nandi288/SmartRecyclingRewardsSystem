using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DropOffPointController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /DropOffPoint/Index
        // Displays all drop-off points
        // ============================================================
        public ActionResult Index()
        {
            var dropOffPoints = _db.DropOffPoints
                .Include(d => d.AssignedOfficer)
                .OrderBy(d => d.Name)
                .ToList();
            return View("ListDropOffPoints", dropOffPoints);
        }

        // ============================================================
        // GET: /DropOffPoint/Create
        // Displays the form to create a new drop-off point
        // ============================================================
        public ActionResult Create()
        {
            // Get list of Collection Officers for assignment
            ViewBag.AssignedOfficerId = new SelectList(
                _db.Users.Where(u => u.Role == "CollectionOfficer" && u.IsActive)
                    .OrderBy(u => u.LastName),
                "Id",
                "FullName"
            );
            return View("AddNewDropOffPoint");
        }

        // ============================================================
        // POST: /DropOffPoint/Create
        // Saves the new drop-off point to the database
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(DropOffPoint model)
        {
            if (ModelState.IsValid)
            {
                // Check if drop-off point with same name already exists
                var exists = _db.DropOffPoints.Any(d => d.Name.ToLower() == model.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "A drop-off point with this name already exists.");
                    PopulateOfficerDropdown();
                    return View("AddNewDropOffPoints", model);
                }

                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                _db.DropOffPoints.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Drop-Off Point '{model.Name}' has been created successfully!";
                return RedirectToAction("Index");
            }

            PopulateOfficerDropdown();
            return View("AddNewDropOffPoints", model);
        }

        // ============================================================
        // GET: /DropOffPoint/Edit/5
        // Displays the form to edit a drop-off point
        // ============================================================
        public async Task<ActionResult> Edit(int id)
        {
            var dropOffPoint = await _db.DropOffPoints.FindAsync(id);
            if (dropOffPoint == null)
            {
                TempData["Error"] = "Drop-Off Point not found.";
                return RedirectToAction("Index");
            }

            PopulateOfficerDropdown(dropOffPoint.AssignedOfficerId);
            return View("Edit", dropOffPoint);
        }

        // ============================================================
        // POST: /DropOffPoint/Edit/5
        // Updates the drop-off point in the database
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(DropOffPoint model)
        {
            if (ModelState.IsValid)
            {
                var dropOffPoint = await _db.DropOffPoints.FindAsync(model.DropOffPointId);
                if (dropOffPoint == null)
                {
                    TempData["Error"] = "Drop-Off Point not found.";
                    return RedirectToAction("Index");
                }

                // Check if another drop-off point has the same name
                var exists = _db.DropOffPoints.Any(d =>
                    d.Name.ToLower() == model.Name.ToLower() &&
                    d.DropOffPointId != model.DropOffPointId);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Another drop-off point with this name already exists.");
                    PopulateOfficerDropdown(model.AssignedOfficerId);
                    return View("Edit", model);
                }

                dropOffPoint.Name = model.Name;
                dropOffPoint.Address = model.Address;
                dropOffPoint.City = model.City;
                dropOffPoint.OperatingHours = model.OperatingHours;
                dropOffPoint.AssignedOfficerId = model.AssignedOfficerId;
                dropOffPoint.IsActive = model.IsActive;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Drop-Off Point '{dropOffPoint.Name}' has been updated successfully!";
                return RedirectToAction("Index");
            }

            PopulateOfficerDropdown(model.AssignedOfficerId);
            return View("Edit", model);
        }

        // ============================================================
        // GET: /DropOffPoint/Delete/5
        // Shows confirmation page for deleting a drop-off point
        // ============================================================
        public async Task<ActionResult> Delete(int id)
        {
            var dropOffPoint = await _db.DropOffPoints
                .Include(d => d.AssignedOfficer)
                .FirstOrDefaultAsync(d => d.DropOffPointId == id);

            if (dropOffPoint == null)
            {
                TempData["Error"] = "Drop-Off Point not found.";
                return RedirectToAction("Index");
            }
            return View("Delete", dropOffPoint);
        }

        // ============================================================
        // POST: /DropOffPoint/Delete/5
        // Deletes or deactivates the drop-off point
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var dropOffPoint = await _db.DropOffPoints.FindAsync(id);
            if (dropOffPoint == null)
            {
                TempData["Error"] = "Drop-Off Point not found.";
                return RedirectToAction("Index");
            }

            // Check if any submissions use this drop-off point
            var hasSubmissions = _db.RecyclingSubmissions.Any(s => s.DropOffPointId == id);
            if (hasSubmissions)
            {
                // Instead of deleting, deactivate it
                dropOffPoint.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Info"] = $"Drop-Off Point '{dropOffPoint.Name}' has been deactivated because it has existing submissions.";
            }
            else
            {
                _db.DropOffPoints.Remove(dropOffPoint);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Drop-Off Point '{dropOffPoint.Name}' has been deleted successfully!";
            }

            return RedirectToAction("Index");
        }

        // ============================================================
        // Helper method to populate officer dropdown
        // ============================================================
        private void PopulateOfficerDropdown(string selectedId = null)
        {
            ViewBag.AssignedOfficerId = new SelectList(
                _db.Users.Where(u => u.Role == "CollectionOfficer" && u.IsActive)
                    .OrderBy(u => u.LastName),
                "Id",
                "FullName",
                selectedId
            );
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