using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private async void Register_Click(object sender, RoutedEventArgs e) => await AuthenticateAsync(register: true);

    private async void Login_Click(object sender, RoutedEventArgs e) => await AuthenticateAsync(register: false);

    private async Task AuthenticateAsync(bool register)
    {
        var login = LoginBox.Text.Trim();
        var password = PasswordBox.Password;
        if (login.Length < 3 || password.Length < 8)
        {
            AccountStatus.Text = "Введите логин от 3 символов и пароль от 8 символов.";
            return;
        }

        if (register && password != ConfirmBox.Password)
        {
            AccountStatus.Text = "Пароли не совпадают.";
            return;
        }

        AccountForm.IsEnabled = false;
        AccountStatus.Text = string.Empty;
        try
        {
            if (register)
            {
                using var created = await Api.PostAsJsonAsync("api/auth/register",
                    new { login, password, confirmPassword = ConfirmBox.Password });
                if (!created.IsSuccessStatusCode)
                {
                    AccountStatus.Text = created.StatusCode == HttpStatusCode.Conflict
                        ? "Этот логин уже занят."
                        : "Не удалось зарегистрироваться. Проверьте введённые данные.";
                    return;
                }
            }

            using var signedIn = await Api.PostAsJsonAsync("api/auth/login", new { login, password });
            if (signedIn.StatusCode == HttpStatusCode.Unauthorized)
            {
                AccountStatus.Text = "Неверный логин или пароль.";
                return;
            }
            signedIn.EnsureSuccessStatusCode();
            var token = await signedIn.Content.ReadFromJsonAsync<SignInResponse>();
            if (string.IsNullOrWhiteSpace(token?.AccessToken))
                throw new HttpRequestException("API did not return an access token.");

            using var profile = new HttpRequestMessage(HttpMethod.Get, "api/auth/me");
            profile.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var response = await Api.SendAsync(profile);
            response.EnsureSuccessStatusCode();
            var account = await response.Content.ReadFromJsonAsync<Account>()
                ?? throw new HttpRequestException("API did not return an account.");

            Api.DefaultRequestHeaders.Authorization = profile.Headers.Authorization;
            AccountName.Text = account.Role == "Admin" ? $"{account.Login} (администратор)" : account.Login;
            AccountForm.Visibility = Visibility.Collapsed;
            AccountSession.Visibility = Visibility.Visible;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            AccountStatus.Text = "Не удалось войти. Проверьте, запущен ли API.";
        }
        finally
        {
            PasswordBox.Clear();
            ConfirmBox.Clear();
            AccountForm.IsEnabled = true;
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        Api.DefaultRequestHeaders.Authorization = null;
        AccountName.Text = string.Empty;
        AccountSession.Visibility = Visibility.Collapsed;
        AccountForm.Visibility = Visibility.Visible;
        AccountStatus.Text = string.Empty;
    }

    private sealed record SignInResponse(string AccessToken);
    private sealed record Account(string Login, string Role);
}
