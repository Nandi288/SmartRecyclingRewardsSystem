using SmartRecyclingRewardsSystem.Models;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminCollectionEventController : Controller
    {
        private readonly ApplicationDbContext _db = new ApplicationDbContext();

        // ============================================================
        // GET: /AdminCollectionEvent
        // List all collection events (active + inactive)
        // ============================================================
        public ActionResult Index()
        {
            var events = _db.CollectionEvents
                .Include(e => e.DropOffPoint)
                .Include(e => e.Registrations)
                .OrderByDescending(e => e.EventDate)
                .ToList();

            return View(events);
        }

        // ============================================================
        // GET: /AdminCollectionEvent/Create
        // ============================================================
        public ActionResult Create()
        {
            ViewBag.DropOffPoints = _db.DropOffPoints
                .Where(d => d.IsActive)
                .OrderBy(d => d.Name)
                .ToList();

            return View(new CollectionEvent());
        }

        // ============================================================
        // POST: /AdminCollectionEvent/Create
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CollectionEvent model)
        {
            if (model.EndTime.HasValue && model.EndTime.Value <= model.EventDate)
            {
                ModelState.AddModelError("EndTime", "End time must be after the event start time.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DropOffPoints = _db.DropOffPoints.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
                return View(model);
            }

            _db.CollectionEvents.Add(model);
            _db.SaveChanges();

            TempData["Success"] = string.Format("Event \"{0}\" was created successfully.", model.Name);
            return RedirectToAction("Index");
        }

        // ============================================================
        // GET: /AdminCollectionEvent/Edit/5
        // ============================================================
        public ActionResult Edit(int id)
        {
            var evt = _db.CollectionEvents.Find(id);
            if (evt == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }

            ViewBag.DropOffPoints = _db.DropOffPoints
                .Where(d => d.IsActive || d.DropOffPointId == evt.DropOffPointId)
                .OrderBy(d => d.Name)
                .ToList();

            return View(evt);
        }

        // ============================================================
        // POST: /AdminCollectionEvent/Edit/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, CollectionEvent model)
        {
            if (id != model.CollectionEventId)
            {
                TempData["Error"] = "Event ID mismatch.";
                return RedirectToAction("Index");
            }

            if (model.EndTime.HasValue && model.EndTime.Value <= model.EventDate)
            {
                ModelState.AddModelError("EndTime", "End time must be after the event start time.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.DropOffPoints = _db.DropOffPoints.Where(d => d.IsActive).OrderBy(d => d.Name).ToList();
                return View(model);
            }

            var evt = _db.CollectionEvents.Find(id);
            if (evt == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }

            evt.Name = model.Name;
            evt.Description = model.Description;
            evt.EventDate = model.EventDate;
            evt.EndTime = model.EndTime;
            evt.DropOffPointId = model.DropOffPointId;
            evt.MaxRegistrations = model.MaxRegistrations;
            evt.IsActive = model.IsActive;

            _db.SaveChanges();

            TempData["Success"] = string.Format("Event \"{0}\" was updated.", evt.Name);
            return RedirectToAction("Index");
        }

        // ============================================================
        // POST: /AdminCollectionEvent/Deactivate/5
        // Soft-delete pattern, same as Drop-Off Points (keeps registration history intact)
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Deactivate(int id)
        {
            var evt = _db.CollectionEvents.Find(id);
            if (evt == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }

            evt.IsActive = false;
            _db.SaveChanges();

            TempData["Success"] = string.Format("Event \"{0}\" was deactivated.", evt.Name);
            return RedirectToAction("Index");
        }

        // ============================================================
        // POST: /AdminCollectionEvent/Reactivate/5
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reactivate(int id)
        {
            var evt = _db.CollectionEvents.Find(id);
            if (evt == null)
            {
                TempData["Error"] = "Event not found.";
                return RedirectToAction("Index");
            }

            evt.IsActive = true;
            _db.SaveChanges();

            TempData["Success"] = string.Format("Event \"{0}\" was reactivated.", evt.Name);
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) _db.Dispose();
            base.Dispose(disposing);
        }
    }
}