using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;

namespace ShopProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductSizesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductSizesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /ProductSizes/Index/5
        public async Task<IActionResult> Index(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            ViewBag.ProductId = productId;
            ViewBag.ProductName = product.Name;
            ViewBag.HasSizes = product.HasSizes;

            return View(product.Sizes.OrderBy(s => s.Size).ToList());
        }

        // POST: /ProductSizes/ToggleHasSizes
        [HttpPost]
        public async Task<IActionResult> ToggleHasSizes(int productId)
        {
            var product = await _context.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // Если выключаем размеры — скрываем все размеры
            if (product.HasSizes)
            {
                foreach (var size in product.Sizes)
                {
                    size.IsAvailable = false;
                }
                product.HasSizes = false;
                TempData["Success"] = "Размеры для товара отключены. Все размеры скрыты.";
            }
            else
            {
                // Если включаем размеры — делаем доступными только те, у которых есть остаток
                foreach (var size in product.Sizes)
                {
                    if (size.StockQuantity > 0)
                    {
                        size.IsAvailable = true;
                    }
                }
                product.HasSizes = true;
                TempData["Success"] = "Размеры для товара включены. Доступны размеры с остатком > 0.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { productId });
        }

        // POST: /ProductSizes/Add
        [HttpPost]
        public async Task<IActionResult> Add(int productId, string size, int stockQuantity = 10)
        {
            if (string.IsNullOrWhiteSpace(size))
            {
                TempData["Error"] = "Введите размер.";
                return RedirectToAction(nameof(Index), new { productId });
            }

            var product = await _context.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product == null) return NotFound();

            // Приводим к верхнему регистру и убираем пробелы
            var trimmedSize = size.Trim().ToUpper();

            // Проверяем, есть ли уже такой размер у этого товара
            var existingSize = product.Sizes.FirstOrDefault(s => s.Size == trimmedSize);
            if (existingSize != null)
            {
                TempData["Error"] = $"Размер «{trimmedSize}» уже существует. Если нужно изменить количество — отредактируйте его.";
                return RedirectToAction(nameof(Index), new { productId });
            }

            // Добавляем новый размер
            product.Sizes.Add(new ProductSize
            {
                Size = trimmedSize,
                StockQuantity = stockQuantity,
                IsAvailable = true
            });

            // Если у товара были выключены размеры — включаем
            if (!product.HasSizes)
            {
                product.HasSizes = true;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Размер «{trimmedSize}» добавлен (в наличии: {stockQuantity} шт.)";
            return RedirectToAction(nameof(Index), new { productId });
        }

        // POST: /ProductSizes/UpdateStock
        [HttpPost]
        public async Task<IActionResult> UpdateStock(int sizeId, int productId, int stockQuantity)
        {
            var size = await _context.ProductSizes.FindAsync(sizeId);
            if (size == null) return NotFound();

            size.StockQuantity = stockQuantity;

            // Если остаток стал 0 — скрываем размер
            if (stockQuantity <= 0)
            {
                size.IsAvailable = false;
                TempData["Warning"] = $"Остаток для размера «{size.Size}» установлен в 0. Размер скрыт.";
            }
            else
            {
                // Если остаток > 0 и товар имеет размеры — делаем размер доступным
                var product = await _context.Products
                    .Include(p => p.Sizes)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product != null && product.HasSizes)
                {
                    size.IsAvailable = true;
                }
                TempData["Success"] = $"Количество для размера «{size.Size}» обновлено: {stockQuantity} шт.";
            }

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { productId });
        }

        // POST: /ProductSizes/ToggleAvailability
        [HttpPost]
        public async Task<IActionResult> ToggleAvailability(int sizeId, int productId)
        {
            var size = await _context.ProductSizes
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == sizeId);

            if (size == null) return NotFound();

            // Проверяем, есть ли остаток перед скрытием
            if (size.IsAvailable && size.StockQuantity <= 0)
            {
                TempData["Error"] = $"Нельзя сделать доступным размер «{size.Size}» — остаток 0 шт. Сначала пополните склад.";
                return RedirectToAction(nameof(Index), new { productId });
            }

            size.IsAvailable = !size.IsAvailable;
            await _context.SaveChangesAsync();

            // ===== АВТОМАТИЧЕСКОЕ ОБНОВЛЕНИЕ HasSizes (РАСКОММЕНТИРОВАНО) =====
            var product = await _context.Products
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == productId);

            if (product != null)
            {
                // HasSizes = true, если есть хотя бы один доступный размер
                product.HasSizes = product.Sizes.Any(s => s.IsAvailable);
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = size.IsAvailable
                ? $"Размер «{size.Size}» снова доступен!"
                : $"Размер «{size.Size}» скрыт!";

            return RedirectToAction(nameof(Index), new { productId });
        }

        // POST: /ProductSizes/Delete
        [HttpPost]
        public async Task<IActionResult> Delete(int sizeId, int productId)
        {
            var size = await _context.ProductSizes
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == sizeId);

            if (size == null) return NotFound();

            var inCart = await _context.CartItems
                .AnyAsync(ci => ci.ProductId == productId && ci.Size == size.Size);

            var inOrder = await _context.OrderItems
                .AnyAsync(oi => oi.ProductId == productId && oi.Size == size.Size);

            if (inCart || inOrder)
            {
                size.IsAvailable = false;
                await _context.SaveChangesAsync();

                // ===== АВТОМАТИЧЕСКОЕ ОБНОВЛЕНИЕ HasSizes (РАСКОММЕНТИРОВАНО) =====
                var product = await _context.Products
                    .Include(p => p.Sizes)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product != null)
                {
                    product.HasSizes = product.Sizes.Any(s => s.IsAvailable);
                    await _context.SaveChangesAsync();
                }

                TempData["Error"] = $"Размер «{size.Size}» скрыт (используется в корзинах/заказах).";
            }
            else
            {
                _context.ProductSizes.Remove(size);
                await _context.SaveChangesAsync();

                // ===== АВТОМАТИЧЕСКОЕ ОБНОВЛЕНИЕ HasSizes (РАСКОММЕНТИРОВАНО) =====
                var product = await _context.Products
                    .Include(p => p.Sizes)
                    .FirstOrDefaultAsync(p => p.Id == productId);

                if (product != null)
                {
                    product.HasSizes = product.Sizes.Any(s => s.IsAvailable);
                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = $"Размер «{size.Size}» удалён.";
            }

            return RedirectToAction(nameof(Index), new { productId });
        }
    }
}