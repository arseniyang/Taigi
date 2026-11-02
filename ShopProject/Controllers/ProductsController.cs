using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShopProject.Data;
using ShopProject.Models;

namespace ShopProject.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public ProductsController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Products
        public async Task<IActionResult> Index(string searchQuery, int page = 1)
        {
            const int pageSize = 20;

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .Include(p => p.Sizes)
                .AsQueryable();

            // ===== ПОИСК =====
            if (!string.IsNullOrWhiteSpace(searchQuery))
            {
                searchQuery = searchQuery.Trim().ToLower();
                query = query.Where(p =>
                    (p.Name != null && p.Name.ToLower().Contains(searchQuery)) ||
                    (p.Article != null && p.Article.ToLower().Contains(searchQuery)) ||
                    (p.Category != null && p.Category.Name.ToLower().Contains(searchQuery))
                );
                ViewBag.SearchQuery = searchQuery;
            }

            // ===== СОРТИРОВКА =====
            query = query.OrderByDescending(p => p.CreatedAt);

            // ===== ПАГИНАЦИЯ =====
            var paginatedProducts = await PaginatedList<Product>.CreateAsync(query, page, pageSize);

            return View(paginatedProducts);
        }

        // GET: Products/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images.OrderBy(i => i.DisplayOrder))
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            return View(product);
        }

        // GET: Products/Create
        public IActionResult Create()
        {
            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name");
            return View(new ProductEditViewModel());
        }

        // POST: Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create(ProductEditViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name", model.CategoryId);
                return View(model);
            }

            var product = new Product
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                OldPrice = model.OldPrice,
                Article = model.Article,
                Color = model.Color,
                Material = model.Material,
                Country = model.Country,
                CategoryId = model.CategoryId,
                IsActive = true,
                HasSizes = model.HasSizes,
                CreatedAt = DateTime.Now
            };

            // 1) Сохраняем товар, чтобы получить Id
            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // 2) Добавляем картинки в контекст (без сохранения)
            await SaveImagesAsync(product.Id, model.NewImageFiles, model.NewImageUrls);

            // 3) Если товар без размеров — создаём "OneSize"
            if (!model.HasSizes)
            {
                _context.ProductSizes.Add(new ProductSize
                {
                    ProductId = product.Id,
                    Size = "OneSize",
                    StockQuantity = 0,
                    IsAvailable = true
                });
            }

            // 4) ОДИН save в конце — сохраняет картинки + OneSize (если создавали)
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Товар успешно создан!";
            return RedirectToAction(nameof(Index));
        }

        // GET: Products/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var model = new ProductEditViewModel
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                OldPrice = product.OldPrice,
                Article = product.Article,
                Color = product.Color,
                Material = product.Material,
                Country = product.Country,
                CategoryId = product.CategoryId,
                HasSizes = product.HasSizes,
                ExistingImages = product.Images.OrderBy(i => i.DisplayOrder).ToList()
            };

            ViewBag.CategoryId = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(_context.Categories, "Id", "Name", product.CategoryId);
            return View(model);
        }

        // POST: Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
                return View(model);
            }

            try
            {
                var product = await _context.Products
                    .Include(p => p.Images)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (product == null) return NotFound();

                // Обновляем основные поля
                product.Name = model.Name;
                product.Description = model.Description;
                product.Price = model.Price;
                product.OldPrice = model.OldPrice;
                product.Article = model.Article;
                product.Color = model.Color;
                product.Material = model.Material;
                product.Country = model.Country;
                product.CategoryId = model.CategoryId;
                product.HasSizes = model.HasSizes;

                // ===== ОБНОВЛЯЕМ ПОРЯДОК ИЗОБРАЖЕНИЙ =====
                if (!string.IsNullOrEmpty(model.ImageOrder))
                {
                    var orderIds = model.ImageOrder
                        .Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(int.Parse)
                        .ToList();

                    for (int i = 0; i < orderIds.Count; i++)
                    {
                        var image = product.Images.FirstOrDefault(img => img.Id == orderIds[i]);
                        if (image != null)
                        {
                            image.DisplayOrder = i;
                        }
                    }
                }

                // Удаляем отмеченные изображения
                await DeleteImagesAsync(product, model.ImagesToDelete);

                // Добавляем новые
                await SaveImagesAsync(product.Id, model.NewImageFiles, model.NewImageUrls);

                await _context.SaveChangesAsync();

                TempData["Success"] = "Товар успешно обновлён!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка: {ex.Message}";
                ViewBag.CategoryId = new SelectList(_context.Categories, "Id", "Name", model.CategoryId);
                return View(model);
            }
        }

        // GET: Products/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.CartItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var isInOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            ViewBag.IsInOrders = isInOrders;

            var isInCarts = product.CartItems != null && product.CartItems.Any();
            ViewBag.IsInCarts = isInCarts;

            return View(product);
        }

        // POST: Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.Sizes)
                .Include(p => p.CartItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null) return NotFound();

            var isInOrders = await _context.OrderItems.AnyAsync(oi => oi.ProductId == id);
            if (isInOrders)
            {
                TempData["Error"] = "Товар не может быть удалён, так как он есть в заказах.";
                return RedirectToAction(nameof(Index));
            }

            var isInCarts = product.CartItems != null && product.CartItems.Any();
            if (isInCarts)
            {
                TempData["Error"] = "Товар не может быть удалён, так как он есть в корзинах пользователей.";
                return RedirectToAction(nameof(Index));
            }
            var isInWishlist = await _context.WishlistItems.AnyAsync(w => w.ProductId == id);
            if (isInWishlist)
            {
                TempData["Error"] = "Товар не может быть удалён, так как он есть в списке желаемого пользователей.";
                return RedirectToAction(nameof(Index));
            }

            // Удаляем размеры
            if (product.Sizes.Any())
            {
                _context.ProductSizes.RemoveRange(product.Sizes);
            }

            foreach (var image in product.Images.Where(i => !i.IsExternal))
            {
                DeleteFile(image.ImageUrl);
            }

            _context.Products.Remove(product);
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Товар успешно удалён.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Products/Hide/5
        [HttpPost]
        public async Task<IActionResult> Hide(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = false;
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Товар скрыт из каталога.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Products/Unhide/5
        [HttpPost]
        public async Task<IActionResult> Unhide(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();

            product.IsActive = true;
            await _context.SaveChangesAsync();

            TempData["Success"] = "✅ Товар снова виден в каталоге.";
            return RedirectToAction(nameof(Index));
        }

        // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========

        private async Task SaveImagesAsync(int productId, List<IFormFile>? files, List<string>? urls)
        {
            if (files != null && files.Any())
            {
                int order = 0;
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        var fileName = await SaveFileAsync(file);
                        _context.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = $"/images/products/{fileName}",
                            ProductId = productId,
                            DisplayOrder = order++,
                            IsExternal = false
                        });
                    }
                }
            }

            if (urls != null && urls.Any())
            {
                int order = files?.Count ?? 0;
                foreach (var url in urls)
                {
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        _context.ProductImages.Add(new ProductImage
                        {
                            ImageUrl = url,
                            ProductId = productId,
                            DisplayOrder = order++,
                            IsExternal = true
                        });
                    }
                }
            }
        }

        private async Task DeleteImagesAsync(Product product, List<int>? imagesToDelete)
        {
            if (imagesToDelete == null || !imagesToDelete.Any()) return;

            var toRemove = product.Images.Where(i => imagesToDelete.Contains(i.Id)).ToList();
            if (!toRemove.Any()) return;

            foreach (var image in toRemove)
            {
                if (!image.IsExternal && !string.IsNullOrEmpty(image.ImageUrl))
                {
                    DeleteFile(image.ImageUrl);
                }
                _context.ProductImages.Remove(image);
            }
        }

        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var fileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
            var path = Path.Combine(_webHostEnvironment.WebRootPath, "images", "products", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return fileName;
        }

        private void DeleteFile(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;

            var path = Path.Combine(_webHostEnvironment.WebRootPath, imageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
        }
    }
}