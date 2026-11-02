using ShopProject.Models;

namespace ShopProject.Services
{
    public interface IStockService
    {
        /// <summary>
        /// Проверяет, достаточно ли товара на складе
        /// </summary>
        Task<bool> IsStockAvailableAsync(int productId, string size, int quantity);

        /// <summary>
        /// Резервирует (списывает) товар со склада
        /// </summary>
        Task<bool> ReserveStockAsync(int productId, string size, int quantity);

        /// <summary>
        /// Возвращает товар на склад
        /// </summary>
        Task<bool> ReleaseStockAsync(int productId, string size, int quantity);

        /// <summary>
        /// Получает текущий остаток по размеру
        /// </summary>
        Task<int> GetStockQuantityAsync(int productId, string size);

        /// <summary>
        /// Проверяет все позиции корзины и возвращает список ошибок
        /// </summary>
        Task<List<StockValidationError>> ValidateCartItemsAsync(List<CartItem> cartItems);
    }

    public class StockValidationError
    {
        public string ProductName { get; set; } = string.Empty;
        public string? Size { get; set; }
        public int RequestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public string Message => $"Товар «{ProductName}»{(string.IsNullOrEmpty(Size) ? "" : $" (размер {Size})")}: запрошено {RequestedQuantity}, доступно {AvailableQuantity} шт.";
    }
}