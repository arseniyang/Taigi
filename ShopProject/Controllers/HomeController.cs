using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;

namespace ShopProject.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Новинки (последние 8)
            var newProducts = await _context.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8)
                .ToListAsync();

            // Хиты (для примера — берём случайные или по популярности)
            var hitProducts = await _context.Products
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id) // временно
                .Take(4)
                .ToListAsync();

            ViewBag.NewProducts = newProducts;
            ViewBag.HitProducts = hitProducts;

            return View();
        }
        public IActionResult PageNotFound(int? statusCode = null)
        {
            ViewBag.StatusCode = statusCode ?? 404;
            return View();
        }
    }
}