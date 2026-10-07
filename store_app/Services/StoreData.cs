using store_app.Models;

namespace store_app.Services;

public static class StoreData
{
    public static event EventHandler? ProductsChanged;

    public static IReadOnlyList<CategoryItem> Categories { get; } =
    [
        new() { Name = "Бүх ангилал", Accent = "#2563EB" },
        new() { Name = "Шинэ бараа", Accent = "#0EA5E9" },
        new() { Name = "Хямдрал", Accent = "#FBBF24" },
        new() { Name = "Ундаа", Accent = "#38BDF8" },
        new() { Name = "Гоймон", Accent = "#60A5FA" },
        new() { Name = "Амттан", Accent = "#7C3AED" },
        new() { Name = "Лаазалсан", Accent = "#0F766E" },
        new() { Name = "Амтлагч", Accent = "#EA580C" },
        new() { Name = "Өдөр тутам", Accent = "#1D4ED8" },
        new() { Name = "Хүнс", Accent = "#0891B2" },
    ];

    public static IReadOnlyList<Product> Products { get; private set; } =
    [
        new()
        {
            Id = "1058465",
            Name = "Skipjack туна загас, 142гр",
            Brand = "Field Day",
            Category = "Лаазалсан",
            Unit = "ш",
            Image = "product_01.png",
            Price = 6477,
            OldPrice = 12990,
            Badge = "-50%",
            Rating = 4.8,
        },
        new()
        {
            Id = "1058732",
            Name = "Органик Angel Hair гоймон",
            Brand = "Field Day",
            Category = "Гоймон",
            Unit = "ш",
            Image = "product_02.png",
            Price = 7477,
            OldPrice = 14990,
            Badge = "-50%",
            Rating = 4.7,
        },
        new()
        {
            Id = "1058447",
            Name = "Ногоон чавганы жүүс",
            Brand = "Woongjin",
            Category = "Ундаа",
            Unit = "ш",
            Image = "product_03.png",
            Price = 8997,
            OldPrice = 12990,
            Badge = "Хямдрал",
            Rating = 4.6,
        },
        new()
        {
            Id = "1276650",
            Name = "Dr. Aloe алоэ жүүс",
            Brand = "Woongjin",
            Category = "Ундаа",
            Unit = "ш",
            Image = "product_04.png",
            Price = 8997,
            OldPrice = 12990,
            Badge = "Шинэ",
            Rating = 4.9,
        },
        new()
        {
            Id = "1255283",
            Name = "Нимбэгтэй хийжүүлсэн ус 350мл",
            Brand = "Victoria",
            Category = "Ундаа",
            Unit = "ш",
            Image = "product_05.png",
            Price = 2277,
            OldPrice = 2999,
            Badge = "-24%",
            Rating = 4.5,
        },
        new()
        {
            Id = "1059486",
            Name = "Трубочки гоймон, 400гр",
            Brand = "СОЮЗ",
            Category = "Гоймон",
            Unit = "ш",
            Image = "product_06.png",
            Price = 2397,
            OldPrice = 3199,
            Badge = "Өдөр бүр",
            Rating = 4.4,
        },
        new()
        {
            Id = "1059413",
            Name = "Чихэртэй өтгөрүүлсэн сүү 920гр",
            Brand = "Главпродукт",
            Category = "Өдөр тутам",
            Unit = "ш",
            Image = "product_07.png",
            Price = 8977,
            OldPrice = 10990,
            Badge = "-18%",
            Rating = 4.7,
        },
        new()
        {
            Id = "1059597",
            Name = "Шар будаа, 800гр",
            Brand = "Царь",
            Category = "Өдөр тутам",
            Unit = "ш",
            Image = "product_08.png",
            Price = 4497,
            OldPrice = null,
            Badge = "Эрэлттэй",
            Rating = 4.6,
        },
        new()
        {
            Id = "1059594",
            Name = "Гурвалжин будаа, 800гр",
            Brand = "Царь",
            Category = "Өдөр тутам",
            Unit = "ш",
            Image = "product_09.png",
            Price = 6697,
            OldPrice = null,
            Badge = "Эрэлттэй",
            Rating = 4.8,
        },
        new()
        {
            Id = "1277660",
            Name = "Шоколадны цуглуулга",
            Brand = "Mieszko",
            Category = "Амттан",
            Unit = "ш",
            Image = "product_10.png",
            Price = 12670,
            OldPrice = null,
            Badge = "Бэлэг",
            Rating = 4.9,
        },
        new()
        {
            Id = "1058142",
            Name = "Хан боргоцойны жүүс 0.97л",
            Brand = "J7",
            Category = "Ундаа",
            Unit = "ш",
            Image = "product_11.png",
            Price = 6997,
            OldPrice = null,
            Badge = "100%",
            Rating = 4.6,
        },
        new()
        {
            Id = "1058140",
            Name = "Олон жимст жүүс 0.97л",
            Brand = "J7",
            Category = "Ундаа",
            Unit = "ш",
            Image = "product_12.png",
            Price = 6997,
            OldPrice = null,
            Badge = "100%",
            Rating = 4.7,
        },
    ];

    public static async Task InitializeAsync()
    {
        await StoreDatabase.InitializeAsync();
        await RefreshProductsAsync();
    }

    public static async Task RefreshProductsAsync()
    {
        var products = await StoreDatabase.GetProductsAsync();
        if (products.Count > 0)
        {
            Products = products;
            ProductsChanged?.Invoke(null, EventArgs.Empty);
        }
    }

    public static async Task UpdateProductPricingAsync(Product product, decimal price, decimal? oldPrice, int discountPercent)
    {
        await StoreDatabase.UpdateProductPricingAsync(product.Id, price, oldPrice, discountPercent);
        await RefreshProductsAsync();
    }

    public static IReadOnlyList<RewardEvent> RewardEvents { get; } =
    [
        new() { Title = "Худалдан авалт", Detail = "Захиалга #FM-2406", Points = "+128" },
        new() { Title = "QR уншуулалт", Detail = "Салбарын касс", Points = "+42" },
        new() { Title = "Оноо ашиглалт", Detail = "Сагсны хөнгөлөлт", Points = "-2,000" },
    ];
}
