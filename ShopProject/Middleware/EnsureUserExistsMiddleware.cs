using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ShopProject.Data;
using System.Security.Claims;

namespace ShopProject.Middleware
{
    public class EnsureUserExistsMiddleware
    {
        private readonly RequestDelegate _next;

        public EnsureUserExistsMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.User.Identity.IsAuthenticated)
            {
                using var scope = serviceProvider.CreateScope();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                    {
                        await context.SignOutAsync(IdentityConstants.ApplicationScheme);
                        context.Session.Clear();
                        context.Response.Redirect("/");
                        return;
                    }
                }
            }

            await _next(context);
        }
    }
}