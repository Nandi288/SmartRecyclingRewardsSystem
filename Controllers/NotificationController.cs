using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.ViewModels;

namespace SmartRecyclingRewardsSystem.Controllers
{
    [Authorize]
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _db;

        public NotificationController()
        {
            _db = new ApplicationDbContext();
        }

        // GET: Notification/Index
        public async Task<ActionResult> Index()
        {
            var userId = User.Identity.GetUserId();
            var userManager = new UserManager<ApplicationUser>(
                new UserStore<ApplicationUser>(_db));
            var user = await userManager.FindByIdAsync(userId);

            // Get all notifications for the user, ordered by newest first
            var notifications = _db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();

            // Mark all notifications as read when viewing the list
            var unreadNotifications = notifications.Where(n => !n.IsRead).ToList();
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            if (unreadNotifications.Any())
            {
                await _db.SaveChangesAsync();
            }

            var viewModel = notifications.Select(n => new NotificationViewModel
            {
                NotificationId = n.NotificationId,
                Title = n.Title,
                Message = n.Message,
                NotificationType = n.NotificationType,
                IsRead = n.IsRead, // Now all will be true after marking
                EmailSent = n.EmailSent,
                SmsSent = n.SmsSent,
                CreatedAt = n.CreatedAt,
                RecyclingSubmissionId = n.RecyclingSubmissionId
            }).ToList();

            ViewBag.UserName = user?.FullName ?? user?.UserName;
            ViewBag.UnreadCount = 0; // Reset after marking all as read

            return View(viewModel);
        }

        // GET: Notification/Unread (for the bell dropdown preview)
        [HttpGet]
        public ActionResult Unread()
        {
            var userId = User.Identity.GetUserId();

            var unreadNotifications = _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(10)
                .Select(n => new NotificationViewModel
                {
                    NotificationId = n.NotificationId,
                    Title = n.Title,
                    Message = n.Message.Length > 80 ? n.Message.Substring(0, 80) + "..." : n.Message,
                    NotificationType = n.NotificationType,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    RecyclingSubmissionId = n.RecyclingSubmissionId
                })
                .ToList();

            var count = _db.Notifications.Count(n => n.UserId == userId && !n.IsRead);

            return Json(new { count, notifications = unreadNotifications }, JsonRequestBehavior.AllowGet);
        }

        // POST: Notification/MarkAsRead/{id}
        [HttpPost]
        public async Task<ActionResult> MarkAsRead(int id)
        {
            var userId = User.Identity.GetUserId();
            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null || notification.UserId != userId)
            {
                return Json(new { success = false, message = "Notification not found." });
            }

            notification.IsRead = true;
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // POST: Notification/MarkAllAsRead
        [HttpPost]
        public async Task<ActionResult> MarkAllAsRead()
        {
            var userId = User.Identity.GetUserId();

            var unreadNotifications = _db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToList();

            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
            }

            await _db.SaveChangesAsync();

            return Json(new { success = true, count = unreadNotifications.Count });
        }

        // GET: Notification/GetUnreadCount
        [HttpGet]
        public ActionResult GetUnreadCount()
        {
            var userId = User.Identity.GetUserId();
            var count = _db.Notifications
                .Count(n => n.UserId == userId && !n.IsRead);

            return Json(new { unreadCount = count }, JsonRequestBehavior.AllowGet);
        }

        // GET: Notification/Delete/{id}
        [HttpPost]
        public async Task<ActionResult> Delete(int id)
        {
            var userId = User.Identity.GetUserId();
            var notification = await _db.Notifications.FindAsync(id);

            if (notification == null || notification.UserId != userId)
            {
                return Json(new { success = false, message = "Notification not found." });
            }

            _db.Notifications.Remove(notification);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: Notification/ClearAll
        [HttpPost]
        public async Task<ActionResult> ClearAll()
        {
            var userId = User.Identity.GetUserId();

            var notifications = _db.Notifications
                .Where(n => n.UserId == userId)
                .ToList();

            _db.Notifications.RemoveRange(notifications);
            await _db.SaveChangesAsync();

            return Json(new { success = true });
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