using System.Globalization;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using store_app.Models;

namespace store_app.Services;

public static class StoreDatabase
{
    const string DatabaseName = "store_app.db3";
    static readonly SemaphoreSlim InitializeGate = new(1, 1);
    static bool initialized;

    static string DatabasePath => Path.Combine(FileSystem.AppDataDirectory, DatabaseName);

    public static async Task InitializeAsync()
    {
        if (initialized)
        {
            return;
        }

        await InitializeGate.WaitAsync();
        try
        {
            if (initialized)
            {
                return;
            }

            SQLitePCL.Batteries_V2.Init();
            Directory.CreateDirectory(FileSystem.AppDataDirectory);

            await using var connection = CreateConnection();
            await connection.OpenAsync();
            await CreateSchemaAsync(connection);
            await MigrateUserSchemaAsync(connection);

            if (await CountAsync(connection, "Products") == 0)
            {
                await SeedProductsAsync(connection);
            }

            if (await CountAsync(connection, "Users") == 0)
            {
                await SeedUsersAsync(connection);
            }

            initialized = true;
        }
        finally
        {
            InitializeGate.Release();
        }
    }

    public static async Task<List<Product>> GetProductsAsync()
    {
        await InitializeAsync();

        var products = new List<Product>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Brand, Category, Unit, Image, Price, OldPrice, Badge, Rating, Stock, Description, DiscountPercent
            FROM Products
            ORDER BY DiscountPercent DESC, Name COLLATE NOCASE
            """;

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            products.Add(ReadProduct(reader));
        }

        return products;
    }

    public static async Task UpdateProductPricingAsync(string productId, decimal price, decimal? oldPrice, int discountPercent)
    {
        await InitializeAsync();

        var cleanPrice = Math.Max(0, price);
        decimal? cleanOldPrice = oldPrice.HasValue && oldPrice.Value > cleanPrice
            ? oldPrice.Value
            : null;
        var cleanDiscount = Math.Clamp(discountPercent, 0, 95);
        var badge = cleanDiscount > 0 ? $"-{cleanDiscount}%" : "Эрэлттэй";

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE Products
            SET Price = $price,
                OldPrice = $oldPrice,
                DiscountPercent = $discountPercent,
                Badge = $badge,
                UpdatedAt = $updatedAt
            WHERE Id = $id
            """;
        command.Parameters.AddWithValue("$price", (double)cleanPrice);
        command.Parameters.AddWithValue("$oldPrice", cleanOldPrice.HasValue ? (double)cleanOldPrice.Value : DBNull.Value);
        command.Parameters.AddWithValue("$discountPercent", cleanDiscount);
        command.Parameters.AddWithValue("$badge", badge);
        command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        command.Parameters.AddWithValue("$id", productId);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<UserAccount?> SignInAsync(string phone, string password)
    {
        await InitializeAsync();

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Name, Phone, LoyaltyPoints
            FROM Users
            WHERE lower(Phone) = lower($phone) AND Password = $password
            LIMIT 1
            """;
        command.Parameters.AddWithValue("$phone", phone.Trim());
        command.Parameters.AddWithValue("$password", password);

        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UserAccount
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Phone = reader.GetString(2),
            LoyaltyPoints = reader.GetInt32(3),
        };
    }

    public static async Task SetUserLoyaltyPointsAsync(string userId, int points)
    {
        await InitializeAsync();

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "UPDATE Users SET LoyaltyPoints = $points WHERE Id = $id";
        command.Parameters.AddWithValue("$points", Math.Max(0, points));
        command.Parameters.AddWithValue("$id", userId);
        await command.ExecuteNonQueryAsync();
    }

    public static async Task<List<RewardEvent>> GetRewardHistoryAsync(string userId)
    {
        await InitializeAsync();

        var events = new List<RewardEvent>();
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Title, Detail, Points, Source, CreatedAt
            FROM LoyaltyTransactions
            WHERE UserId = $userId
            ORDER BY CreatedAt DESC, Id DESC
            LIMIT 20
            """;
        command.Parameters.AddWithValue("$userId", userId);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var points = reader.GetInt32(2);
            var source = reader.GetString(3);
            events.Add(new RewardEvent
            {
                Title = reader.GetString(0),
                Detail = reader.GetString(1),
                Points = points >= 0 ? $"+{points:#,0}" : $"-{Math.Abs(points):#,0}",
                Icon = source == "game" ? "★" : source == "spend" ? "P" : "₮",
                Time = FormatHistoryTime(reader.GetString(4)),
                PointsColor = points >= 0 ? "#2563EB" : "#EF4444",
                IconBackground = source == "game" ? "#FFF3C4" : "#DBEAFE",
            });
        }

        return events;
    }

    public static async Task AddLoyaltyTransactionAsync(string userId, string title, string detail, int points, string source)
    {
        await InitializeAsync();

        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO LoyaltyTransactions (UserId, Title, Detail, Points, Source, CreatedAt)
            VALUES ($userId, $title, $detail, $points, $source, $createdAt)
            """;
        command.Parameters.AddWithValue("$userId", userId);
        command.Parameters.AddWithValue("$title", title);
        command.Parameters.AddWithValue("$detail", detail);
        command.Parameters.AddWithValue("$points", points);
        command.Parameters.AddWithValue("$source", source);
        command.Parameters.AddWithValue("$createdAt", DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
        await command.ExecuteNonQueryAsync();
    }

    static SqliteConnection CreateConnection()
    {
        return new SqliteConnection($"Data Source={DatabasePath}");
    }

    static async Task CreateSchemaAsync(SqliteConnection connection)
    {
        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS Products (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Brand TEXT NOT NULL,
                Category TEXT NOT NULL,
                Unit TEXT NOT NULL,
                Image TEXT NOT NULL,
                Price REAL NOT NULL,
                OldPrice REAL NULL,
                Badge TEXT NOT NULL,
                Rating REAL NOT NULL,
                Stock INTEGER NOT NULL,
                Description TEXT NOT NULL,
                DiscountPercent INTEGER NOT NULL,
                UpdatedAt TEXT NOT NULL
            )
            """);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS Users (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                LoyaltyPoints INTEGER NOT NULL
            )
            """);

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS LoyaltyTransactions (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                UserId TEXT NOT NULL,
                Title TEXT NOT NULL,
                Detail TEXT NOT NULL,
                Points INTEGER NOT NULL,
                Source TEXT NOT NULL,
                CreatedAt TEXT NOT NULL
            )
            """);
    }

    static async Task SeedProductsAsync(SqliteConnection connection)
    {
        await using var stream = await FileSystem.OpenAppPackageFileAsync("products_seed.json");
        var products = await JsonSerializer.DeserializeAsync<List<ProductSeedItem>>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        await using var transaction = connection.BeginTransaction();
        foreach (var product in products)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT OR REPLACE INTO Products
                    (Id, Name, Brand, Category, Unit, Image, Price, OldPrice, Badge, Rating, Stock, Description, DiscountPercent, UpdatedAt)
                VALUES
                    ($id, $name, $brand, $category, $unit, $image, $price, $oldPrice, $badge, $rating, $stock, $description, $discountPercent, $updatedAt)
                """;
            command.Parameters.AddWithValue("$id", product.Id);
            command.Parameters.AddWithValue("$name", product.Name);
            command.Parameters.AddWithValue("$brand", product.Brand);
            command.Parameters.AddWithValue("$category", product.Category);
            command.Parameters.AddWithValue("$unit", product.Unit);
            command.Parameters.AddWithValue("$image", product.Image);
            command.Parameters.AddWithValue("$price", (double)product.Price);
            command.Parameters.AddWithValue("$oldPrice", product.OldPrice.HasValue ? (double)product.OldPrice.Value : DBNull.Value);
            command.Parameters.AddWithValue("$badge", product.Badge);
            command.Parameters.AddWithValue("$rating", product.Rating);
            command.Parameters.AddWithValue("$stock", product.Stock);
            command.Parameters.AddWithValue("$description", product.Description);
            command.Parameters.AddWithValue("$discountPercent", product.DiscountPercent);
            command.Parameters.AddWithValue("$updatedAt", DateTimeOffset.Now.ToString("O", CultureInfo.InvariantCulture));
            await command.ExecuteNonQueryAsync();
        }

        await transaction.CommitAsync();
    }

    static async Task SeedUsersAsync(SqliteConnection connection)
    {
        await InsertUserAsync(connection, "user-demo", "Бат-Эрдэнэ", "99112233", "1234", 34820);
    }

    static async Task InsertUserAsync(SqliteConnection connection, string id, string name, string phone, string password, int loyaltyPoints)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT OR REPLACE INTO Users (Id, Name, Phone, Password, LoyaltyPoints)
            VALUES ($id, $name, $phone, $password, $loyaltyPoints)
            """;
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$phone", phone);
        command.Parameters.AddWithValue("$password", password);
        command.Parameters.AddWithValue("$loyaltyPoints", loyaltyPoints);
        await command.ExecuteNonQueryAsync();
    }

    static async Task MigrateUserSchemaAsync(SqliteConnection connection)
    {
        var hasLegacyStaffColumn = false;
        await using (var command = connection.CreateCommand())
        {
            command.CommandText = "PRAGMA table_info(Users)";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                if (string.Equals(reader.GetString(1), "IsStaff", StringComparison.OrdinalIgnoreCase))
                {
                    hasLegacyStaffColumn = true;
                    break;
                }
            }
        }

        if (!hasLegacyStaffColumn)
        {
            return;
        }

        await ExecuteAsync(connection, """
            CREATE TABLE IF NOT EXISTS Users_New (
                Id TEXT PRIMARY KEY,
                Name TEXT NOT NULL,
                Phone TEXT NOT NULL UNIQUE,
                Password TEXT NOT NULL,
                LoyaltyPoints INTEGER NOT NULL
            )
            """);
        await ExecuteAsync(connection, """
            INSERT OR REPLACE INTO Users_New (Id, Name, Phone, Password, LoyaltyPoints)
            SELECT Id, Name, Phone, Password, LoyaltyPoints
            FROM Users
            WHERE lower(Phone) <> 'staff' AND Id <> 'staff-demo'
            """);
        await ExecuteAsync(connection, "DROP TABLE Users");
        await ExecuteAsync(connection, "ALTER TABLE Users_New RENAME TO Users");
    }

    static async Task ExecuteAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    static async Task<long> CountAsync(SqliteConnection connection, string table)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {table}";
        return (long)(await command.ExecuteScalarAsync() ?? 0L);
    }

    static Product ReadProduct(SqliteDataReader reader)
    {
        return new Product
        {
            Id = reader.GetString(0),
            Name = reader.GetString(1),
            Brand = reader.GetString(2),
            Category = reader.GetString(3),
            Unit = reader.GetString(4),
            Image = reader.GetString(5),
            Price = Convert.ToDecimal(reader.GetDouble(6), CultureInfo.InvariantCulture),
            OldPrice = reader.IsDBNull(7) ? null : Convert.ToDecimal(reader.GetDouble(7), CultureInfo.InvariantCulture),
            Badge = reader.GetString(8),
            Rating = reader.GetDouble(9),
            Stock = reader.GetInt32(10),
            Description = reader.GetString(11),
            DiscountPercent = reader.GetInt32(12),
        };
    }

    static string FormatHistoryTime(string createdAt)
    {
        if (!DateTimeOffset.TryParse(createdAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
        {
            return "Одоо";
        }

        var local = date.ToLocalTime();
        if (local.Date == DateTimeOffset.Now.Date)
        {
            return "Өнөөдөр";
        }

        if (local.Date == DateTimeOffset.Now.AddDays(-1).Date)
        {
            return "Өчигдөр";
        }

        return local.ToString("MM.dd", CultureInfo.InvariantCulture);
    }

    sealed class ProductSeedItem
    {
        public string Id { get; init; } = string.Empty;

        public string Name { get; init; } = string.Empty;

        public string Brand { get; init; } = string.Empty;

        public string Category { get; init; } = string.Empty;

        public string Unit { get; init; } = string.Empty;

        public string Image { get; init; } = string.Empty;

        public decimal Price { get; init; }

        public decimal? OldPrice { get; init; }

        public string Badge { get; init; } = string.Empty;

        public double Rating { get; init; }

        public int Stock { get; init; }

        public string Description { get; init; } = string.Empty;

        public int DiscountPercent { get; init; }
    }
}
