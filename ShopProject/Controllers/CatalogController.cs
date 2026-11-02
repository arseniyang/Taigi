using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;
using System.Security.Claims;

namespace ShopProject.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CatalogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Catalog
        public async Task<IActionResult> Index(
      int? categoryId,
      string? sortBy,
      string? searchQuery,
      int page = 1,
      int pageSize = 12)
        {
            var products = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(p => p.IsActive)
                .AsQueryable();

            // ===== ФИЛЬТР ПО КАТЕГОРИИ =====
            if (categoryId.HasValue)
            {
                products = products.Where(p => p.CategoryId == categoryId.Value);
            }

            // ===== ПОИСК =====
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                var q = searchQuery.Trim().ToLower();
                products = products.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(q)) ||
                    (p.Description != null && p.Description.ToLower().Contains(q)) ||
                    (p.Article != null && p.Article.ToLower().Contains(q))
                );
                ViewBag.SearchQuery = searchQuery;
            }

            // ===== СОРТИРОВКА =====
            sortBy ??= "popular";
            ViewBag.SortBy = sortBy;

            products = sortBy switch
            {
                "price_asc" => products.OrderBy(p => p.Price),
                "price_desc" => products.OrderByDescending(p => p.Price),
                "newest" => products.OrderByDescending(p => p.CreatedAt),
                "popular" or _ => products.OrderByDescending(p => p.Id) // Временная замена
            };

            // ===== ПАГИНАЦИЯ =====
            var totalItems = await products.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await products
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // ===== ДЛЯ ФИЛЬТРОВ =====
            var categories = await _context.Categories.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CurrentCategoryId = categoryId;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentPage = page;

            // ===== ПРОВЕРКА ИЗБРАННОГО ДЛЯ АВТОРИЗОВАННЫХ =====
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var wishlistIds = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .Select(w => w.ProductId)
                    .ToListAsync();

                ViewBag.WishlistIds = wishlistIds;
            }
            else
            {
                ViewBag.WishlistIds = new List<int>();
            }

            return View(items);
        }

        // GET: /Catalog/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Sizes.Where(s => s.IsAvailable && s.StockQuantity > 0))
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null) return NotFound();

            // ===== ПРОВЕРКА ИЗБРАННОГО ДЛЯ АВТОРИЗОВАННЫХ =====
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var isInWishlist = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.ProductId == id);

                ViewBag.IsInWishlist = isInWishlist;
            }
            else
            {
                ViewBag.IsInWishlist = false;
            }

            // ===== РЕКОМЕНДАЦИИ: товары из той же категории =====
            var recommendations = await _context.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id && p.IsActive)
                .OrderByDescending(p => p.Id)
                .Take(4)
                .ToListAsync();

            ViewBag.Recommendations = recommendations;

            return View(product);
        }
    }
}