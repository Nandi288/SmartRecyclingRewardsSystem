//using System;
//using System.Data.Entity;
//using System.Linq;
//using System.Threading.Tasks;
//using System.Web.Mvc;
//using SmartRecyclingRewardsSystem.Models;

//namespace SmartRecyclingRewardsSystem.Controllers
//{
//    [Authorize(Roles = "Admin")]
//    public class SystemConfigController : Controller
//    {
//        private readonly ApplicationDbContext _db = new ApplicationDbContext();

//        // GET: /SystemConfig/Index
//        public async Task<ActionResult> Index()
//        {
//            var settings = await _db.SystemConfigs.OrderBy(s => s.Key).ToListAsync();
//            return View(settings);
//        }

//        // POST: /SystemConfig/Update
//        [HttpPost]
//        [ValidateAntiForgeryToken]
//        public async Task<ActionResult> Update(SystemConfig[] settings)
//        {
//            if (settings == null || settings.Length == 0)
//            {
//                TempData["ErrorMessage"] = "No settings were submitted.";
//                return RedirectToAction("Index");
//            }

//            foreach (var setting in settings)
//            {
//                if (setting.SystemConfigId == 0) continue;

//                var existing = await _db.SystemConfigs.FindAsync(setting.SystemConfigId);
//                if (existing != null)
//                {
//                    existing.Value = setting.Value;
//                    existing.LastUpdated = DateTime.Now;
//                }
//            }

//            await _db.SaveChangesAsync();
//            TempData["SuccessMessage"] = "System settings updated successfully.";
//            return RedirectToAction("Index");
//        }

//        protected override void Dispose(bool disposing)
//        {
//            if (disposing) _db.Dispose();
//            base.Dispose(disposing);
//        }
//    }
//}IGNORE THE ABOVE COMMENT, IT IS NOT RELEVANT TO THE CODE...IT WAS TO HAVE A SAFETY NET IN CASE THE CODE BELOW DIDNT DO WHAT I NEEDED IT TO DO 
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SmartRecyclingRewardsSystem.Models;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SystemConfigController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // GET: /SystemConfig/Index
        public async Task<ActionResult> Index()
        {
            var settings = await _db.SystemConfigs.OrderBy(s => s.Key).ToListAsync();
            return View(settings);
        }

        // POST: /SystemConfig/Update
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Update(SystemConfig[] settings)
        {
            if (settings == null || settings.Length == 0)
            {
                TempData["ErrorMessage"] = "No settings were submitted.";
                return RedirectToAction("Index");
            }

            foreach (var setting in settings)
            {
                if (setting.SystemConfigId == 0) continue;

                var existing = await _db.SystemConfigs.FindAsync(setting.SystemConfigId);
                if (existing != null)
                {
                    existing.Value = setting.Value??string.Empty;
                    existing.LastUpdated = DateTime.Now;
                    // If you want to track who updated:
                    // existing.UpdatedByAdminId = User.Identity.GetUserId();
                }
            }

            try
            {
                await _db.SaveChangesAsync();
                TempData["SuccessMessage"] = "System settings updated successfully.";
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException ex)
            {
                // Build detailed error messages
                var errorMessages = new List<string>();
                foreach (var validationErrors in ex.EntityValidationErrors)
                {
                    foreach (var error in validationErrors.ValidationErrors)
                    {
                        errorMessages.Add($"Property: {error.PropertyName} - Error: {error.ErrorMessage}");
                    }
                }

                // Show errors to user
                TempData["ErrorMessage"] = "Validation failed: " + string.Join("; ", errorMessages);

                // Also log to Output window (View → Output → Debug)
                System.Diagnostics.Debug.WriteLine("=== VALIDATION ERRORS ===");
                foreach (var msg in errorMessages)
                {
                    System.Diagnostics.Debug.WriteLine(msg);
                }
            }

            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}