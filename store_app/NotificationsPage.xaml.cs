namespace store_app;

public partial class NotificationsPage : ContentPage
{
    public NotificationsPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public IReadOnlyList<NotificationItem> Notifications { get; } =
    [
        new("Өнөөдрийн хямдрал эхэллээ", "Ундаа болон өдөр тутмын бараанд шинэ хямдрал нэмэгдлээ.", "Одоо"),
        new("Loyalty оноо нэмэгдлээ", "Сүүлийн худалдан авалтаас +128 оноо таны дансанд орлоо.", "12 мин"),
        new("QR оноо ашиглах боломжтой", "Касс дээр QR кодоо уншуулж оноогоо төлбөртөө ашиглаарай.", "1 цаг"),
        new("Шинэ бараа ирлээ", "Гоймон, жүүс, амттаны шинэ сонголтууд нэмэгдсэн байна.", "Өчигдөр"),
    ];

    async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync("//app/home");
    }
}

public sealed record NotificationItem(string Title, string Detail, string Time);
