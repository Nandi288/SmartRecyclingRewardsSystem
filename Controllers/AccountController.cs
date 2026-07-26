using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using SmartRecyclingRewardsSystem.Data;
using SmartRecyclingRewardsSystem.Models;
using SmartRecyclingRewardsSystem.Services;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace SmartRecyclingRewardsSystem.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationUserManager _userManager;
        private ApplicationSignInManager _signInManager;

        // Used to send the "account created" email once registration succeeds
        private readonly ApplicationDbContext _db = new ApplicationDbContext();
        private readonly NotificationService _notificationService;

        public AccountController()
        {
            _notificationService = new NotificationService(_db);
        }

        public ApplicationUserManager UserManager
        {
            get { return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>(); }
            private set { _userManager = value; }
        }

        public ApplicationSignInManager SignInManager
        {
            get { return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>(); }
            private set { _signInManager = value; }
        }

        private IAuthenticationManager AuthenticationManager
        {
            get { return HttpContext.GetOwinContext().Authentication; }
        }

        // ── GET: /Account/Login ───────────────────────────────────
        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            // If already logged in, redirect away
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        // ── POST: /Account/Login ──────────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            // Block login for deactivated accounts before attempting sign-in
            var existingUser = await UserManager.FindByEmailAsync(model.Email);
            if (existingUser != null && !existingUser.IsActive)
            {
                ModelState.AddModelError("", "This account has been deactivated. Please contact an administrator.");
                return View(model);
            }

            var result = await SignInManager.PasswordSignInAsync(
                model.Email, model.Password, model.RememberMe, shouldLockout: false);

            switch (result)
            {
                case SignInStatus.Success:
                    return RedirectToLocal(returnUrl);

                case SignInStatus.Failure:
                default:
                    ModelState.AddModelError("", "Incorrect email or password. Please try again.");
                    return View(model);
            }
        }

        // ── GET: /Account/Register ────────────────────────────────
        [AllowAnonymous]
        public ActionResult Register()
        {
            if (Request.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // ── POST: /Account/Register ───────────────────────────────
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Register(RegisterViewModel model)
        {
            // Block anyone from registering as Admin via the form
            if (model.Role == "Admin")
            {
                ModelState.AddModelError("Role", "You cannot register as an Administrator.");
                return View(model);
            }

            // Only allow valid roles
            if (model.Role != "Resident" && model.Role != "CollectionOfficer")
            {
                ModelState.AddModelError("Role", "Please select a valid role.");
                return View(model);
            }

            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Address = model.Address,
                    City = model.City,
                    Role = model.Role,
                    ReceiveEmailNotifications = model.ReceiveEmailNotifications,
                    ReceiveSmsNotifications = model.ReceiveSmsNotifications
                };

                var result = await UserManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    // Assign the chosen role
                    await UserManager.AddToRoleAsync(user.Id, model.Role);

                    // Send the "account created" email/notification
                    await _notificationService.NotifyAccountCreatedAsync(user);

                    // Sign them in immediately
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);

                    TempData["Success"] = "Welcome to EcoRewards SA, " + user.FirstName + "!";
                    return RedirectToAction("Index", "Home");
                }

                // Show Identity errors (e.g. password too short, email taken)
                foreach (var error in result.Errors)
                    ModelState.AddModelError("", error);
            }

            return View(model);
        }

        // ── POST: /Account/LogOff ─────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
            return RedirectToAction("Login", "Account");
        }

        // ── Helper ────────────────────────────────────────────────
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction("Index", "Home");
        }

        // Cleans up the database connection this controller opened
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