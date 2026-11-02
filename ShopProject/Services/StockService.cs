using Microsoft.EntityFrameworkCore;
using ShopProject.Data;
using ShopProject.Models;

namespace ShopProject.Services
{
    public class StockService : IStockService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StockService> _logger;

        public StockService(ApplicationDbContext context, ILogger<StockService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> IsStockAvailableAsync(int productId, string size, int quantity)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.Size == size);

            if (productSize == null || !productSize.IsAvailable)
                return false;

            return productSize.StockQuantity >= quantity;
        }

        public async Task<bool> ReserveStockAsync(int productId, string size, int quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(s => s.ProductId == productId && s.Size == size);

                if (productSize == null || !productSize.IsAvailable)
                {
                    _logger.LogWarning($"Попытка резерва: размер {size} для товара {productId} не найден или недоступен");
                    return false;
                }

                if (productSize.StockQuantity < quantity)
                {
                    _logger.LogWarning($"Недостаточно товара: {productSize.StockQuantity} < {quantity}");
                    return false;
                }

                productSize.StockQuantity -= quantity;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Зарезервировано {quantity} шт. товара {productId} (размер {size}). Остаток: {productSize.StockQuantity}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Ошибка при резерве товара {productId} (размер {size})");
                throw;
            }
        }

        public async Task<bool> ReleaseStockAsync(int productId, string size, int quantity)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(s => s.ProductId == productId && s.Size == size);

                if (productSize == null)
                {
                    _logger.LogWarning($"Попытка возврата: размер {size} для товара {productId} не найден");
                    return false;
                }

                productSize.StockQuantity += quantity;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Возвращено {quantity} шт. товара {productId} (размер {size}). Остаток: {productSize.StockQuantity}");
                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Ошибка при возврате товара {productId} (размер {size})");
                throw;
            }
        }

        public async Task<int> GetStockQuantityAsync(int productId, string size)
        {
            var productSize = await _context.ProductSizes
                .FirstOrDefaultAsync(s => s.ProductId == productId && s.Size == size);

            return productSize?.StockQuantity ?? 0;
        }

        public async Task<List<StockValidationError>> ValidateCartItemsAsync(List<CartItem> cartItems)
        {
            var errors = new List<StockValidationError>();

            foreach (var item in cartItems)
            {
                var size = item.Size ?? "OneSize";
                var product = item.Product;

                if (!product.IsActive)
                {
                    errors.Add(new StockValidationError
                    {
                        ProductName = product.Name,
                        Size = size,
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = 0
                    });
                    continue;
                }

                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(s => s.ProductId == product.Id && s.Size == size);

                if (productSize == null || !productSize.IsAvailable)
                {
                    errors.Add(new StockValidationError
                    {
                        ProductName = product.Name,
                        Size = size,
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = 0
                    });
                }
                else if (productSize.StockQuantity < item.Quantity)
                {
                    errors.Add(new StockValidationError
                    {
                        ProductName = product.Name,
                        Size = size,
                        RequestedQuantity = item.Quantity,
                        AvailableQuantity = productSize.StockQuantity
                    });
                }
            }

            return errors;
        }
    }
}