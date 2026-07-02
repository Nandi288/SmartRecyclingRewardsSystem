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
    public class MaterialTypeController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /MaterialType/Index
        // Displays all material types
        // ============================================================
        public ActionResult Index()
        {
            var materials = _db.MaterialTypes
                .OrderBy(m => m.Name)
                .ToList();
            return View("ListMaterials_Index", materials);
        }

        // ============================================================
        // GET: /MaterialType/Create
        // Displays the form to create a new material type
        // ============================================================
        public ActionResult Create()
        {
            return View("AddNewMaterial");
        }

        // ============================================================
        // POST: /MaterialType/Create
        // Saves the new material type to the database
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(MaterialType model)
        {
            if (ModelState.IsValid)
            {
                // Check if material with same name already exists
                var exists = _db.MaterialTypes.Any(m => m.Name.ToLower() == model.Name.ToLower());
                if (exists)
                {
                    ModelState.AddModelError("Name", "A material with this name already exists.");
                    return View("AddNewMaterial", model);
                }

                model.CreatedAt = DateTime.Now;
                model.IsActive = true;

                _db.MaterialTypes.Add(model);
                await _db.SaveChangesAsync();

                TempData["Success"] = $"Material '{model.Name}' has been created successfully!";
                return RedirectToAction("Index");
            }

            return View("AddNewMaterial", model);
        }

        // ============================================================
        // GET: /MaterialType/Edit/5
        // Displays the form to edit a material type
        // ============================================================
        public async Task<ActionResult> Edit(int id)
        {
            var material = await _db.MaterialTypes.FindAsync(id);
            if (material == null)
            {
                TempData["Error"] = "Material not found.";
                return RedirectToAction("Index");
            }
            return View("Edit", material);
        }

        // ============================================================
        // POST: /MaterialType/Edit/5
        // Updates the material type in the database
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(MaterialType model)
        {
            if (ModelState.IsValid)
            {
                var material = await _db.MaterialTypes.FindAsync(model.MaterialTypeId);
                if (material == null)
                {
                    TempData["Error"] = "Material not found.";
                    return RedirectToAction("Index");
                }

                // Check if another material has the same name
                var exists = _db.MaterialTypes.Any(m =>
                    m.Name.ToLower() == model.Name.ToLower() &&
                    m.MaterialTypeId != model.MaterialTypeId);
                if (exists)
                {
                    ModelState.AddModelError("Name", "Another material with this name already exists.");
                    return View("Edit", model);
                }

                material.Name = model.Name;
                material.Description = model.Description;
                material.PointsPerKg = model.PointsPerKg;
                material.CO2SavingPerKg = model.CO2SavingPerKg;
                material.ColourCode = model.ColourCode;
                material.IsActive = model.IsActive;

                await _db.SaveChangesAsync();

                TempData["Success"] = $"Material '{material.Name}' has been updated successfully!";
                return RedirectToAction("Index");
            }

            return View("Edit", model);
        }

        // ============================================================
        // GET: /MaterialType/Delete/5
        // Shows confirmation page for deleting a material type
        // ============================================================
        public async Task<ActionResult> Delete(int id)
        {
            var material = await _db.MaterialTypes.FindAsync(id);
            if (material == null)
            {
                TempData["Error"] = "Material not found.";
                return RedirectToAction("Index");
            }
            return View("Delete", material);
        }

        // ============================================================
        // POST: /MaterialType/Delete/5
        // Deletes the material type from the database
        // ============================================================
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            var material = await _db.MaterialTypes.FindAsync(id);
            if (material == null)
            {
                TempData["Error"] = "Material not found.";
                return RedirectToAction("Index");
            }

            // Check if any submissions use this material
            var hasSubmissions = _db.RecyclingSubmissions.Any(s => s.MaterialTypeId == id);
            if (hasSubmissions)
            {
                // Instead of deleting, deactivate it
                material.IsActive = false;
                await _db.SaveChangesAsync();
                TempData["Info"] = $"Material '{material.Name}' has been deactivated because it has existing submissions.";
            }
            else
            {
                _db.MaterialTypes.Remove(material);
                await _db.SaveChangesAsync();
                TempData["Success"] = $"Material '{material.Name}' has been deleted successfully!";
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