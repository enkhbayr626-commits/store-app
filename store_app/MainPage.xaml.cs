using System.Collections.ObjectModel;
using store_app.Models;
using store_app.Services;

namespace store_app;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshProducts();
    }

    public IReadOnlyList<CategoryItem> Categories => StoreData.Categories;

    public ObservableCollection<Product> Products { get; } = [];

    protected override void OnAppearing()
    {
        base.OnAppearing();
        AppState.CartChanged += OnCartChanged;
        AppState.LoyaltyPointsChanged += OnLoyaltyPointsChanged;
        StoreData.ProductsChanged += OnProductsChanged;
        RefreshHeader();
        RefreshProducts();
    }

    protected override void OnDisappearing()
    {
        AppState.CartChanged -= OnCartChanged;
        AppState.LoyaltyPointsChanged -= OnLoyaltyPointsChanged;
        StoreData.ProductsChanged -= OnProductsChanged;
        base.OnDisappearing();
    }

    async void OnQrClicked(object? sender, TappedEventArgs e)
    {
        await ShowQrOverlayAsync();
    }

    async void OnQrOverlayTapped(object? sender, TappedEventArgs e)
    {
        await HideQrOverlayAsync();
    }

    async void OnLoyaltyCardTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//app/loyalty");
    }

    async void OnPayWithPointsTapped(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync("//app/cart");
    }

    async void OnNotificationClicked(object? sender, TappedEventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(NotificationsPage));
    }

    async void OnAllCategoriesTapped(object? sender, TappedEventArgs e)
    {
        await OpenProductsPageAsync("Бүх ангилал");
    }

    async void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        var categoryName = (sender as Element)?.BindingContext is CategoryItem category
            ? category.Name
            : "Бүх ангилал";

        await OpenProductsPageAsync(categoryName);
    }

    void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        RefreshProducts();
    }

    async void OnAddClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: Product product })
        {
            return;
        }

        AppState.AddProduct(product);
        await Navigation.PushModalAsync(new CartAddedPage(product), false);
    }

    void OnCartChanged(object? sender, EventArgs e)
    {
        RefreshHeader();
    }

    void OnLoyaltyPointsChanged(object? sender, EventArgs e)
    {
        RefreshHeader();
    }

    void OnProductsChanged(object? sender, EventArgs e)
    {
        RefreshProducts();
    }

    void RefreshHeader()
    {
        UserNameLabel.Text = AppState.UserName;
        LoyaltyCardPointsLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
    }

    void RefreshProducts()
    {
        var query = ProductSearchEntry?.Text?.Trim() ?? string.Empty;
        var filteredProducts = string.IsNullOrWhiteSpace(query)
            ? StoreData.Products.Take(12).ToList()
            : StoreData.Products.Where(product => MatchesSearch(product, query)).Take(40).ToList();

        Products.Clear();
        foreach (var product in filteredProducts)
        {
            Products.Add(product);
        }

        CartSummaryLabel.Text = $"{Products.Count} бараа";
        SearchEmptyLabel.IsVisible = Products.Count == 0;
    }

    async Task ShowQrOverlayAsync()
    {
        if (QrOverlay.IsVisible)
        {
            return;
        }

        HomeContent.IsEnabled = false;
        Shell.SetTabBarIsVisible(this, false);
        QrOverlay.IsVisible = true;

        await Task.WhenAll(
            HomeContent.FadeToAsync(0.32, 120),
            HomeContent.ScaleToAsync(0.98, 120),
            QrOverlay.FadeToAsync(1, 140));
    }

    async Task HideQrOverlayAsync()
    {
        if (!QrOverlay.IsVisible)
        {
            return;
        }

        await Task.WhenAll(
            QrOverlay.FadeToAsync(0, 100),
            HomeContent.FadeToAsync(1, 100),
            HomeContent.ScaleToAsync(1, 100));

        QrOverlay.IsVisible = false;
        HomeContent.IsEnabled = true;
        Shell.SetTabBarIsVisible(this, true);
    }

    static Task OpenProductsPageAsync(string categoryName)
    {
        var encodedCategory = Uri.EscapeDataString(categoryName);
        return Shell.Current.GoToAsync($"{nameof(ProductsPage)}?category={encodedCategory}");
    }

    static bool MatchesSearch(Product product, string query)
    {
        return product.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
            || product.Brand.Contains(query, StringComparison.OrdinalIgnoreCase)
            || product.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
            || product.Badge.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
