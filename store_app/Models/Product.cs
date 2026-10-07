namespace store_app.Models;

public sealed class Product
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Brand { get; init; }

    public required string Category { get; init; }

    public required string Unit { get; init; }

    public required string Image { get; init; }

    public decimal Price { get; init; }

    public decimal? OldPrice { get; init; }

    public string Badge { get; init; } = string.Empty;

    public double Rating { get; init; }

    public int Stock { get; init; }

    public string Description { get; init; } = string.Empty;

    public int DiscountPercent { get; init; }

    public bool HasOldPrice => OldPrice.HasValue && OldPrice.Value > Price;

    public string PriceText => Money.Format(Price);

    public string OldPriceText => OldPrice.HasValue ? Money.Format(OldPrice.Value) : string.Empty;

    public string LoyaltyText => $"+{Money.PointsFromPurchase(Price)} оноо";
}

public sealed class UserAccount
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Phone { get; init; }

    public int LoyaltyPoints { get; init; }
}

public sealed class CategoryItem
{
    public required string Name { get; init; }

    public required string Accent { get; init; }
}

public sealed class RewardEvent
{
    public required string Title { get; init; }

    public required string Detail { get; init; }

    public required string Points { get; init; }

    public string Icon { get; init; } = "P";

    public string Time { get; init; } = "Одоо";

    public string PointsColor { get; init; } = "#2563EB";

    public string IconBackground { get; init; } = "#DBEAFE";
}

public static class Money
{
    public static string Format(decimal value)
    {
        return $"{value:#,0}₮";
    }

    public static int PointsFromPurchase(decimal purchaseTotal)
    {
        return (int)Math.Floor(purchaseTotal * 0.01m);
    }
}
