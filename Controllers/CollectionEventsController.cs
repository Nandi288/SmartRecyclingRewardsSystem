using Microsoft.AspNet.Identity;
using SmartRecyclingRewardsSystem.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Resident")]
    public class CollectionEventsController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /CollectionEvent
        // View all upcoming collection events (UC-11 / UC-12 entry point)
        // ============================================================
        public ActionResult Index()
        {
            var residentId = User.Identity.GetUserId();

            var upcomingEvents = _db.CollectionEvents
                .Where(e => e.IsActive && e.EventDate >= DateTime.Now)
                .Include(e => e.DropOffPoint)
                .Include(e => e.Registrations)
                .OrderBy(e => e.EventDate)
                .ToList();

            // Which events has this resident already registered for?
            ViewBag.MyRegisteredEventIds = _db.CollectionEventRegistrations
                .Where(r => r.ResidentId == residentId)
                .Select(r => r.CollectionEventId)
                .ToList();

            return View(upcomingEvents);
        }

        // ============================================================
        // GET: /CollectionEvent/MyEvents
        // View events the resident has registered for (per UC-12 post-condition)
        // ============================================================
        public ActionResult MyEvents()
        {
            var residentId = User.Identity.GetUserId();

            var myRegistrations = _db.CollectionEventRegistrations
                .Where(r => r.ResidentId == residentId)
                .Include(r => r.CollectionEvent)
                .Include(r => r.CollectionEvent.DropOffPoint)
                .OrderBy(r => r.CollectionEvent.EventDate)
                .ToList();

            return View(myRegistrations);
        }

        // ============================================================
        // POST: /CollectionEvent/Register/5
        // Register the current resident for an event
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(int id)
        {
            var residentId = User.Identity.GetUserId();

            var collectionEvent = await _db.CollectionEvents
                .Include(e => e.Registrations)
                .FirstOrDefaultAsync(e => e.CollectionEventId == id);

            if (collectionEvent == null || !collectionEvent.IsActive)
            {
                TempData["Error"] = "This event could not be found or is no longer active.";
                return RedirectToAction("Index");
            }

            if (collectionEvent.EventDate < DateTime.Now)
            {
                TempData["Error"] = "This event has already taken place.";
                return RedirectToAction("Index");
            }

            // Prevent duplicate registration
            bool alreadyRegistered = _db.CollectionEventRegistrations
                .Any(r => r.ResidentId == residentId && r.CollectionEventId == id);

            if (alreadyRegistered)
            {
                TempData["Error"] = "You are already registered for this event.";
                return RedirectToAction("Index");
            }

            // Enforce capacity if MaxRegistrations is set
            if (collectionEvent.MaxRegistrations.HasValue)
            {
                int currentCount = collectionEvent.Registrations.Count;
                if (currentCount >= collectionEvent.MaxRegistrations.Value)
                {
                    TempData["Error"] = "This event is fully booked.";
                    return RedirectToAction("Index");
                }
            }

            var registration = new CollectionEventRegistration
            {
                ResidentId = residentId,
                CollectionEventId = id
            };

            _db.CollectionEventRegistrations.Add(registration);
            await _db.SaveChangesAsync();

            TempData["Success"] = string.Format("You're registered for \"{0}\"!", collectionEvent.Name);
            return RedirectToAction("Index");
        }

        // ============================================================
        // POST: /CollectionEvent/Cancel/5
        // Allow a resident to cancel their registration (nice-to-have, not in UC-12 but common pairing)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Cancel(int id)
        {
            var residentId = User.Identity.GetUserId();

            var registration = await _db.CollectionEventRegistrations
                .FirstOrDefaultAsync(r => r.CollectionEventId == id && r.ResidentId == residentId);

            if (registration == null)
            {
                TempData["Error"] = "Registration not found.";
                return RedirectToAction("MyEvents");
            }

            _db.CollectionEventRegistrations.Remove(registration);
            await _db.SaveChangesAsync();

            TempData["Success"] = "Your registration has been cancelled.";
            return RedirectToAction("MyEvents");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}