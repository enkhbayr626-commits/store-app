using System.Collections.ObjectModel;
using store_app.Models;

namespace store_app.Services;

public static class AppState
{
    public static event EventHandler? CartChanged;

    public static event EventHandler? LoyaltyPointsChanged;

    public static ObservableCollection<CartLine> CartItems { get; } = [];

    public static ObservableCollection<RewardEvent> RewardHistory { get; } = [];

    public static int LoyaltyPoints { get; private set; } = 34820;

    public static UserAccount? CurrentUser { get; private set; }

    public static string UserName => CurrentUser?.Name ?? "Хэрэглэгч";

    public static bool IsUsingPointsForCart { get; private set; }

    static int orderSequence = 2406;

    static AppState()
    {
        RewardHistory.Add(new RewardEvent
        {
            Title = "Худалдан авалт",
            Detail = "Захиалга #FM-2406 • 12,800₮",
            Points = "+128",
            Icon = "₮",
            Time = "Өнөөдөр",
            PointsColor = "#2563EB",
        });
        RewardHistory.Add(new RewardEvent
        {
            Title = "Тоглоомын оноо",
            Detail = "Memory Card • бүх хосыг олсон",
            Points = "+26",
            Icon = "★",
            Time = "Өнөөдөр",
            PointsColor = "#2563EB",
            IconBackground = "#FFF3C4",
        });
        RewardHistory.Add(new RewardEvent
        {
            Title = "Оноо ашиглалт",
            Detail = "Сагсны төлбөрт ашигласан",
            Points = "-2,000",
            Icon = "P",
            Time = "Өчигдөр",
            PointsColor = "#EF4444",
        });

        AddProduct(StoreData.Products[1], notify: false);
        AddProduct(StoreData.Products[4], notify: false);
        AddProduct(StoreData.Products[10], notify: false);
    }

    public static int ItemCount => CartItems.Sum(item => item.Quantity);

    public static decimal CartTotal => CartItems.Sum(item => item.LineTotal);

    public static int PointsToEarn => Money.PointsFromPurchase(CartTotal);

    public static int PointsToUse => IsUsingPointsForCart
        ? Math.Min(LoyaltyPoints, (int)Math.Floor(CartTotal))
        : 0;

    public static decimal PointDiscount => PointsToUse;

    public static decimal PayableTotal => Math.Max(0, CartTotal - PointDiscount);

    public static string CartTotalText => Money.Format(CartTotal);

    public static string PayableTotalText => Money.Format(PayableTotal);

    public static async Task<bool> SignInAsync(string phone, string password)
    {
        await StoreData.InitializeAsync();

        var user = await StoreDatabase.SignInAsync(phone, password);
        if (user is null)
        {
            return false;
        }

        CurrentUser = user;
        LoyaltyPoints = user.LoyaltyPoints;
        IsUsingPointsForCart = false;

        CartItems.Clear();
        SeedDefaultCart();
        await LoadRewardHistoryAsync();

        CartChanged?.Invoke(null, EventArgs.Empty);
        LoyaltyPointsChanged?.Invoke(null, EventArgs.Empty);
        return true;
    }

    public static void AddLoyaltyPoints(int points)
    {
        if (points <= 0)
        {
            return;
        }

        LoyaltyPoints += points;
        PersistLoyaltyPoints();
        LoyaltyPointsChanged?.Invoke(null, EventArgs.Empty);
    }

    public static void AddGameReward(int points, string gameName)
    {
        if (points <= 0)
        {
            return;
        }

        AddLoyaltyPoints(points);
        AddRewardHistory("Тоглоомын оноо", $"{gameName} • тоглоомоос цуглуулсан", $"+{points:#,0}", "★", "#2563EB", "#FFF3C4", points, "game");
    }

    public static void AddGameHistory(int points, string gameName, string detail)
    {
        if (points <= 0)
        {
            return;
        }

        AddRewardHistory("Тоглоомын оноо", $"{gameName} • {detail}", $"+{points:#,0}", "★", "#2563EB", "#FFF3C4", points, "game");
    }

    public static void AddProduct(Product product)
    {
        AddProduct(product, notify: true);
    }

    public static CheckoutResult CompleteCheckout()
    {
        var earnedPoints = PointsToEarn;
        var usedPoints = PointsToUse;
        var orderTotal = CartTotal;

        if (CartItems.Count == 0)
        {
            return new CheckoutResult(0, 0);
        }

        if (usedPoints > 0)
        {
            LoyaltyPoints -= usedPoints;
            PersistLoyaltyPoints();
            LoyaltyPointsChanged?.Invoke(null, EventArgs.Empty);
            AddRewardHistory(
                "Оноо ашиглалт",
                $"Сагсны төлбөрт ашигласан • {Money.Format(orderTotal)}",
                $"-{usedPoints:#,0}",
                "P",
                "#EF4444",
                "#DBEAFE",
                -usedPoints,
                "spend");
        }

        CartItems.Clear();
        AddLoyaltyPoints(earnedPoints);
        AddRewardHistory(
            "Худалдан авалт",
            $"Захиалга #FM-{++orderSequence} • {Money.Format(orderTotal)}",
            $"+{earnedPoints:#,0}",
            "₮",
            "#2563EB",
            "#DBEAFE",
            earnedPoints,
            "purchase");

        IsUsingPointsForCart = false;
        OnCartChanged();
        return new CheckoutResult(earnedPoints, usedPoints);
    }

    public static void ToggleUsePointsForCart()
    {
        IsUsingPointsForCart = !IsUsingPointsForCart && CartItems.Count > 0 && LoyaltyPoints > 0;
        OnCartChanged();
    }

    public static void Increase(CartLine line)
    {
        line.Quantity++;
        OnCartChanged();
    }

    public static void Decrease(CartLine line)
    {
        if (line.Quantity == 1)
        {
            CartItems.Remove(line);
            if (CartItems.Count == 0)
            {
                IsUsingPointsForCart = false;
            }
        }
        else
        {
            line.Quantity--;
        }

        OnCartChanged();
    }

    static void AddProduct(Product product, bool notify)
    {
        CartLine? line = CartItems.FirstOrDefault(item => item.Product.Id == product.Id);
        if (line is null)
        {
            CartItems.Add(new CartLine(product, 1));
        }
        else
        {
            line.Quantity++;
        }

        if (notify)
        {
            OnCartChanged();
        }
    }

    static void OnCartChanged()
    {
        CartChanged?.Invoke(null, EventArgs.Empty);
    }

    static void SeedDefaultCart()
    {
        if (StoreData.Products.Count == 0)
        {
            return;
        }

        AddProduct(StoreData.Products[Math.Min(1, StoreData.Products.Count - 1)], notify: false);
        AddProduct(StoreData.Products[Math.Min(4, StoreData.Products.Count - 1)], notify: false);
        AddProduct(StoreData.Products[Math.Min(10, StoreData.Products.Count - 1)], notify: false);
    }

    static async Task LoadRewardHistoryAsync()
    {
        RewardHistory.Clear();

        if (CurrentUser is null)
        {
            return;
        }

        var events = await StoreDatabase.GetRewardHistoryAsync(CurrentUser.Id);
        foreach (var rewardEvent in events)
        {
            RewardHistory.Add(rewardEvent);
        }

        if (RewardHistory.Count > 0)
        {
            return;
        }

        AddRewardHistory("Худалдан авалт", "Захиалга #FM-2406 • 12,800₮", "+128", "₮", "#2563EB", "#DBEAFE", 128, "purchase");
        AddRewardHistory("Тоглоомын оноо", "Memory Card • бүх хосыг олсон", "+26", "★", "#2563EB", "#FFF3C4", 26, "game");
        AddRewardHistory("Оноо ашиглалт", "Сагсны төлбөрт ашигласан", "-2,000", "P", "#EF4444", "#DBEAFE", -2000, "spend");
    }

    static void PersistLoyaltyPoints()
    {
        if (CurrentUser is null)
        {
            return;
        }

        _ = StoreDatabase.SetUserLoyaltyPointsAsync(CurrentUser.Id, LoyaltyPoints);
    }

    static void AddRewardHistory(
        string title,
        string detail,
        string points,
        string icon,
        string pointsColor,
        string iconBackground = "#DBEAFE",
        int pointsValue = 0,
        string source = "purchase")
    {
        RewardHistory.Insert(0, new RewardEvent
        {
            Title = title,
            Detail = detail,
            Points = points,
            Icon = icon,
            Time = "Одоо",
            PointsColor = pointsColor,
            IconBackground = iconBackground,
        });

        while (RewardHistory.Count > 20)
        {
            RewardHistory.RemoveAt(RewardHistory.Count - 1);
        }

        if (CurrentUser is not null && pointsValue != 0)
        {
            _ = StoreDatabase.AddLoyaltyTransactionAsync(CurrentUser.Id, title, detail, pointsValue, source);
        }
    }
}

public sealed record CheckoutResult(int EarnedPoints, int UsedPoints);
