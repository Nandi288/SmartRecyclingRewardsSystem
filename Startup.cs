using Microsoft.AspNet.Identity;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using SmartRecyclingRewardsSystem.Models;

[assembly: OwinStartup(typeof(SmartRecyclingRewardsSystem.Startup))]

namespace SmartRecyclingRewardsSystem
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Register the DbContext so OWIN can create it per request
            app.CreatePerOwinContext(ApplicationDbContext.Create);
            app.CreatePerOwinContext<ApplicationUserManager>(ApplicationUserManager.Create);
            app.CreatePerOwinContext<ApplicationSignInManager>(ApplicationSignInManager.Create);

            // Cookie-based authentication
            app.UseCookieAuthentication(new CookieAuthenticationOptions
            {
                AuthenticationType = DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                ExpireTimeSpan = System.TimeSpan.FromMinutes(30),
                SlidingExpiration = true
            });
        }
    }
}