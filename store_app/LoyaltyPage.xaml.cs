using store_app.Models;
using store_app.Services;

namespace store_app;

public partial class LoyaltyPage : ContentPage
{
    public LoyaltyPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public IReadOnlyList<RewardEvent> RewardEvents => AppState.RewardHistory;

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppState.LoyaltyPointsChanged += OnLoyaltyPointsChanged;
        PointBalanceLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
    }

    protected override void OnDisappearing()
    {
        AppState.LoyaltyPointsChanged -= OnLoyaltyPointsChanged;
        base.OnDisappearing();
    }

    void OnLoyaltyPointsChanged(object? sender, EventArgs e)
    {
        PointBalanceLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
    }

    async void OnMemoryGameTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(MemoryGamePage));
    }

    async void OnFlappyGameTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(GamePage));
    }
}
