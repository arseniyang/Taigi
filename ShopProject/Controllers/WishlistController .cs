using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;
using System.Security.Claims;

namespace ShopProject.Controllers
{
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Wishlist
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await _context.WishlistItems
                .Include(w => w.Product)
                    .ThenInclude(p => p.Images)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();

            return View(items);
        }

        // POST: /Wishlist/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, string returnUrl = null)
        {
            if (!User.Identity.IsAuthenticated)
            {
                TempData["WishlistInfo"] = "Войдите в аккаунт, чтобы добавить в избранное";
                return RedirectToPage("/Account/Login", new { area = "Identity", returnUrl = returnUrl ?? Request.Headers["Referer"].ToString() });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _context.Products.FindAsync(productId);

            if (product == null)
            {
                TempData["WishlistError"] = "Товар не найден";
                return RedirectToAction("Index", "Catalog");
            }

            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

            if (!exists)
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = productId,
                    AddedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                TempData["WishlistSuccess"] = $"Товар «{product.Name}» добавлен в избранное";
            }
            else
            {
                // Если уже есть — удаляем (toggle)
                var item = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["WishlistSuccess"] = $"Товар «{product.Name}» удалён из избранного";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Catalog");
        }

        // POST: /Wishlist/Remove
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Remove(int productId, string returnUrl = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var item = await _context.WishlistItems
                .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

            if (item != null)
            {
                _context.WishlistItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["WishlistSuccess"] = "Товар удалён из избранного";
            }

            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // POST: /Wishlist/Clear
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var items = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .ToListAsync();

            if (items.Any())
            {
                _context.WishlistItems.RemoveRange(items);
                await _context.SaveChangesAsync();
                TempData["WishlistSuccess"] = "Список избранного очищен";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle([FromBody] ToggleRequest request)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new
                {
                    success = false,
                    redirect = "/Identity/Account/Login?returnUrl=" + Request.Headers["Referer"].ToString()
                });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var product = await _context.Products.FindAsync(request.ProductId);

            if (product == null)
            {
                return Json(new { success = false, message = "Товар не найден" });
            }

            var exists = await _context.WishlistItems
                .AnyAsync(w => w.UserId == userId && w.ProductId == request.ProductId);

            if (exists)
            {
                var item = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId);
                if (item != null)
                {
                    _context.WishlistItems.Remove(item);
                    await _context.SaveChangesAsync();
                }
                return Json(new { success = true, message = "Товар удалён из избранного" });
            }
            else
            {
                _context.WishlistItems.Add(new WishlistItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    AddedAt = DateTime.Now
                });
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Товар добавлен в избранное" });
            }
        }

        public class ToggleRequest
        {
            public int ProductId { get; set; }
        }
    }
}