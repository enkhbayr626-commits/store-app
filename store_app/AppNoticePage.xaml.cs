namespace store_app;

public partial class AppNoticePage : ContentPage
{
    bool isClosing;

    public AppNoticePage(string title, string message, string highlight = "", string icon = "✓")
    {
        InitializeComponent();

        TitleLabel.Text = title;
        MessageLabel.Text = message;
        HighlightLabel.Text = highlight;
        HighlightCard.IsVisible = !string.IsNullOrWhiteSpace(highlight);
        IconLabel.Text = icon;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await Task.WhenAll(
            Overlay.FadeToAsync(1, 130),
            DialogCard.ScaleToAsync(1, 150, Easing.CubicOut));
    }

    async void OnBackdropTapped(object? sender, TappedEventArgs e)
    {
        await CloseAsync();
    }

    async void OnOkClicked(object? sender, EventArgs e)
    {
        await CloseAsync();
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
            DialogCard.ScaleToAsync(0.96, 100, Easing.CubicIn));

        if (Navigation.ModalStack.Contains(this))
        {
            await Navigation.PopModalAsync(false);
        }
    }
}
