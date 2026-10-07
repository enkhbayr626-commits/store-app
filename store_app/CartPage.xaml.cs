using store_app.Models;
using store_app.Services;

namespace store_app;

public partial class CartPage : ContentPage
{
    public CartPage()
    {
        InitializeComponent();
        CartItemsView.ItemsSource = AppState.CartItems;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppState.CartChanged += OnCartChanged;
        RefreshTotals();
    }

    protected override void OnDisappearing()
    {
        AppState.CartChanged -= OnCartChanged;
        base.OnDisappearing();
    }

    void OnIncreaseClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: CartLine line })
        {
            AppState.Increase(line);
        }
    }

    void OnDecreaseClicked(object? sender, EventArgs e)
    {
        if (sender is Button { BindingContext: CartLine line })
        {
            AppState.Decrease(line);
        }
    }

    void OnUsePointsClicked(object? sender, EventArgs e)
    {
        AppState.ToggleUsePointsForCart();
    }

    async void OnCheckoutClicked(object? sender, EventArgs e)
    {
        if (AppState.ItemCount == 0)
        {
            await ShowCheckoutDialogAsync("Сагс хоосон байна", "Бараа нэмсний дараа захиалгаа үргэлжлүүлээрэй.", string.Empty, false);
            return;
        }

        var result = AppState.CompleteCheckout();
        var pointSummary = result.UsedPoints > 0
            ? $"+{result.EarnedPoints:#,0} оноо · -{result.UsedPoints:#,0} оноо ашиглав"
            : $"+{result.EarnedPoints:#,0} оноо";

        await ShowCheckoutDialogAsync(
            "Захиалга баталгаажлаа",
            "Түүх хэсэгт худалдан авалт болон ашигласан оноо нэмэгдлээ.",
            pointSummary,
            true);
    }

    async void OnCheckoutDialogOkClicked(object? sender, EventArgs e)
    {
        await HideCheckoutDialogAsync();
    }

    void OnCartChanged(object? sender, EventArgs e)
    {
        RefreshTotals();
    }

    void RefreshTotals()
    {
        CartHeaderLabel.Text = $"{AppState.ItemCount} бараа сонгосон";
        EarnPreviewLabel.Text = AppState.IsUsingPointsForCart
            ? $"Оноо ашиглаад +{AppState.PointsToEarn:#,0} loyalty оноо авна"
            : $"Энэ сагсаас +{AppState.PointsToEarn:#,0} loyalty оноо авна";
        CartTotalLabel.Text = AppState.PayableTotalText;
        CartPointLabel.Text = $"+{AppState.PointsToEarn:#,0}";
        UsePointsCard.IsVisible = AppState.ItemCount > 0 && AppState.LoyaltyPoints > 0;
        UsePointsHintLabel.Text = AppState.IsUsingPointsForCart
            ? $"{AppState.PointsToUse:#,0} оноо төлбөрөөс хасагдана"
            : $"{AppState.LoyaltyPoints:#,0} онооноос төлбөрт ашиглана";
        UsePointsButton.Text = AppState.IsUsingPointsForCart ? "Болих" : "Ашиглах";
        UsePointsButton.BackgroundColor = AppState.IsUsingPointsForCart
            ? Color.FromArgb("#2563EB")
            : Color.FromArgb("#FFFFFF");
        UsePointsButton.TextColor = AppState.IsUsingPointsForCart
            ? Colors.White
            : Color.FromArgb("#2563EB");
        DiscountRow.IsVisible = AppState.PointsToUse > 0;
        CartDiscountLabel.Text = $"-{AppState.PointsToUse:#,0}₮";
    }

    async Task ShowCheckoutDialogAsync(string title, string message, string pointsText, bool isSuccess)
    {
        CheckoutDialogTitleLabel.Text = title;
        CheckoutDialogMessageLabel.Text = message;
        CheckoutDialogIconLabel.Text = isSuccess ? "✓" : "!";
        CheckoutDialogPointLabel.Text = pointsText;
        CheckoutDialogPointBadge.IsVisible = !string.IsNullOrWhiteSpace(pointsText);
        CheckoutOverlay.IsVisible = true;
        CheckoutDialogCard.Scale = 0.96;

        await Task.WhenAll(
            CheckoutOverlay.FadeToAsync(1, 140),
            CheckoutDialogCard.ScaleToAsync(1, 140, Easing.CubicOut));
    }

    async Task HideCheckoutDialogAsync()
    {
        if (!CheckoutOverlay.IsVisible)
        {
            return;
        }

        await Task.WhenAll(
            CheckoutOverlay.FadeToAsync(0, 100),
            CheckoutDialogCard.ScaleToAsync(0.96, 100, Easing.CubicIn));

        CheckoutOverlay.IsVisible = false;
    }
}
