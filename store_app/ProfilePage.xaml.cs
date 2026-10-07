using store_app.Services;

namespace store_app;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ProfileNameLabel.Text = AppState.UserName;
        ProfileSubtitleLabel.Text = "QR loyalty идэвхтэй";
        ProfilePointLabel.Text = $"{AppState.LoyaltyPoints:#,0}";
    }

    async void OnProfileActionTapped(object? sender, TappedEventArgs e)
    {
        var action = (sender as Element)?.AutomationId;

        switch (action)
        {
            case "games":
                await Shell.Current.GoToAsync(nameof(MemoryGamePage));
                break;
            case "personal":
                await ShowProfileDialogAsync(
                    "ID",
                    "Хувийн мэдээлэл",
                    "Таны үндсэн бүртгэлийн мэдээлэл",
                    $"Нэр: {AppState.UserName}",
                    "Loyalty: Идэвхтэй",
                    $"Оноо: {AppState.LoyaltyPoints:#,0}");
                break;
            case "address":
                await ShowProfileDialogAsync(
                    "Х",
                    "Хүргэлтийн хаяг",
                    "Захиалга хүргэгдэх үндсэн бүс",
                    "Улаанбаатар хот",
                    "60 минутын хүргэлтийн бүс",
                    "Хаяг database холболтын дараа хадгалагдана.");
                break;
            case "payment":
                await ShowProfileDialogAsync(
                    "₮",
                    "Төлбөрийн арга",
                    "Төлбөр хийх боломжууд",
                    "Карт, QR, loyalty оноо",
                    "1 оноо = 1₮ байдлаар сагсанд ашиглана.",
                    "Одоогоор demo төлөвтэй.");
                break;
            case "orders":
                await ShowProfileDialogAsync(
                    "#",
                    "Миний захиалгууд",
                    "Сүүлийн захиалгын товч мэдээлэл",
                    "Нийт захиалга: 12",
                    "Сүүлийн захиалга: FM-2406",
                    "Дэлгэрэнгүй түүх database холболтын дараа харагдана.");
                break;
        }
    }

    async void OnProfileDialogOkClicked(object? sender, EventArgs e)
    {
        await HideProfileDialogAsync();
    }

    async void OnProfileDialogBackdropTapped(object? sender, TappedEventArgs e)
    {
        await HideProfileDialogAsync();
    }

    async Task ShowProfileDialogAsync(string icon, string title, string subtitle, string primary, string secondary, string meta)
    {
        ProfileDialogIconLabel.Text = icon;
        ProfileDialogTitleLabel.Text = title;
        ProfileDialogSubtitleLabel.Text = subtitle;
        ProfileDialogPrimaryLabel.Text = primary;
        ProfileDialogSecondaryLabel.Text = secondary;
        ProfileDialogMetaLabel.Text = meta;

        ProfileDialogOverlay.IsVisible = true;
        ProfileDialogCard.Scale = 0.96;

        await Task.WhenAll(
            ProfileDialogOverlay.FadeToAsync(1, 140),
            ProfileDialogCard.ScaleToAsync(1, 140, Easing.CubicOut));
    }

    async Task HideProfileDialogAsync()
    {
        if (!ProfileDialogOverlay.IsVisible)
        {
            return;
        }

        await Task.WhenAll(
            ProfileDialogOverlay.FadeToAsync(0, 100),
            ProfileDialogCard.ScaleToAsync(0.96, 100, Easing.CubicIn));

        ProfileDialogOverlay.IsVisible = false;
    }
}
