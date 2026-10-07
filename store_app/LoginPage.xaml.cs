using store_app.Services;

namespace store_app;

public partial class LoginPage : ContentPage
{
    bool isBusy;

    public LoginPage()
    {
        InitializeComponent();
    }

    async void OnLoginClicked(object? sender, EventArgs e)
    {
        await SignInAsync(PhoneEntry.Text ?? string.Empty, PasswordEntry.Text ?? string.Empty);
    }

    async void OnCreateAccountClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(
            new AppNoticePage("Бүртгэл үүсгэх", "Одоогоор demo хэрэглэгчээр нэвтрээд app-аа туршина.", "99112233 / 1234", "ID"),
            false);
    }

    async Task SignInAsync(string phone, string password)
    {
        if (isBusy)
        {
            return;
        }

        isBusy = true;
        ErrorLabel.IsVisible = false;
        LoginButton.IsEnabled = false;
        LoginButton.Text = "Уншиж байна...";

        try
        {
            var signedIn = await AppState.SignInAsync(phone, password);
            if (!signedIn)
            {
                ErrorLabel.IsVisible = true;
                return;
            }

            var shell = new AppShell();
            Application.Current!.Windows[0].Page = shell;
        }
        finally
        {
            isBusy = false;
            LoginButton.IsEnabled = true;
            LoginButton.Text = "Нэвтрэх";
        }
    }
}
