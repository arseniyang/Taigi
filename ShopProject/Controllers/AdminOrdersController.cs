using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;
using ShopProject.Services;
using System.Security.Claims;

namespace ShopProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStockService _stockService; // <-- ДОБАВЛЕНО

        public AdminOrdersController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IStockService stockService) // <-- ДОБАВЛЕН ПАРАМЕТР
        {
            _context = context;
            _userManager = userManager;
            _stockService = stockService;
        }

        // GET: /AdminOrders
        public async Task<IActionResult> Index(string status = null)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            ViewBag.Statuses = new List<string> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            ViewBag.CurrentStatus = status;

            return View(orders);
        }

        // GET: /AdminOrders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.PromoCode)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.StatusHistory)
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            ViewBag.Statuses = new List<string> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };

            return View(order);
        }

        // POST: /AdminOrders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, string status, string note = null, bool returnStock = true)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems) // <-- ДОБАВЛЕНО
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            var oldStatus = order.Status;

            // ===== ЕСЛИ СТАТУС МЕНЯЕТСЯ НА "CANCELLED" =====
            if (status == "Cancelled" && oldStatus != "Cancelled")
            {
                if (returnStock)
                {
                    // Возвращаем остатки на склад
                    foreach (var item in order.OrderItems)
                    {
                        var size = item.Size ?? "OneSize";
                        await _stockService.ReleaseStockAsync(item.ProductId, size, item.Quantity);
                    }
                    note = (note ?? "") + " (товары возвращены на склад)";
                }
                else
                {
                    note = (note ?? "") + " (товары НЕ возвращены на склад)";
                }
            }

            // Меняем статус
            order.Status = status;

            // Добавляем запись в историю
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            order.StatusHistory ??= new List<OrderStatusHistory>();
            order.StatusHistory.Add(new OrderStatusHistory
            {
                Status = status,
                ChangedAt = DateTime.Now,
                ChangedByUserId = userId,
                Note = note ?? $"Статус изменен с {oldStatus} на {status}"
            });

            await _context.SaveChangesAsync();

            TempData["Success"] = $"✔ Статус заказа #{order.Id} изменён на {OrderStatus.GetRussianName(status)}";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /AdminOrders/UpdateTracking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateTracking(int id, string trackingNumber)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();

            order.TrackingNumber = trackingNumber;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"✔ Трек-номер для заказа #{order.Id} обновлён";
            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /AdminOrders/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.StatusHistory)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
                return NotFound();

            // ===== ЕСЛИ ЗАКАЗ НЕ БЫЛ ОТМЕНЁН — ВОЗВРАЩАЕМ ОСТАТКИ =====
            if (order.Status != "Cancelled")
            {
                foreach (var item in order.OrderItems)
                {
                    var size = item.Size ?? "OneSize";
                    await _stockService.ReleaseStockAsync(item.ProductId, size, item.Quantity);
                }
            }

            // Удаляем связанные данные
            if (order.StatusHistory != null)
                _context.OrderStatusHistories.RemoveRange(order.StatusHistory);

            if (order.OrderItems != null)
                _context.OrderItems.RemoveRange(order.OrderItems);

            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Заказ успешно удален. Товары возвращены на склад.";
            return RedirectToAction(nameof(Index));
        }
    }
}