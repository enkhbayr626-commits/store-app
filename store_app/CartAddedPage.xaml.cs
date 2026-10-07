using store_app.Models;

namespace store_app;

public partial class CartAddedPage : ContentPage
{
    bool isClosing;

    public CartAddedPage(Product product)
    {
        InitializeComponent();

        ProductImage.Source = product.Image;
        BrandLabel.Text = product.Brand;
        CategoryLabel.Text = product.Category;
        ProductNameLabel.Text = product.Name;
        PriceLabel.Text = product.PriceText;
        LoyaltyLabel.Text = product.LoyaltyText;
        SummaryLabel.Text = $"Сагсанд {product.Unit} нэгжээр нэмэгдсэн";
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 130),
            SheetCard.TranslateToAsync(0, 0, 160, Easing.CubicOut));
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        await CloseAsync();
    }

    async void OnContinueClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
    }

    async void OnGoToCartClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
        await Shell.Current.GoToAsync("//app/cart");
    }

    async Task CloseAsync()
    {
        if (isClosing)
        {
            return;
        }

        isClosing = true;
        await Task.WhenAll(
            Overlay.FadeToAsync(0, 100),
            SheetCard.TranslateToAsync(0, 34, 120, Easing.CubicIn));

        if (Navigation.ModalStack.Contains(this))
        {
            await Navigation.PopModalAsync(false);
        }
    }
}
