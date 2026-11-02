namespace ShopProject.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Processing = "Processing";
        public const string Shipped = "Shipped";
        public const string Delivered = "Delivered";
        public const string Cancelled = "Cancelled";

        public static string GetRussianName(string status)
        {
            return status switch
            {
                Pending => "Ожидает подтверждения",
                Processing => "В обработке",
                Shipped => "Отправлен",
                Delivered => "Доставлен",
                Cancelled => "Отменён",
                _ => status
            };
        }

        public static string GetBadgeClass(string status)
        {
            return status switch
            {
                Pending => "status-pending",
                Processing => "status-processing",
                Shipped => "status-shipped",
                Delivered => "status-delivered",
                Cancelled => "status-cancelled",
                _ => ""
            };
        }
    }
}