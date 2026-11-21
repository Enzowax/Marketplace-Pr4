using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Marketplace.Wpf;

public partial class ReviewsWindow : Window
{
    private readonly HttpClient api;
    private readonly int productId;
    private readonly string productName;
    private string? login;

    public ReviewsWindow(HttpClient api, MainWindow.ProductItem product)
    {
        InitializeComponent();
        this.api = api;
        productId = product.Id;
        productName = product.Name;
        Editor.Visibility = Visibility.Collapsed;
        Loaded += async (_, _) => await RunAsync(async () =>
        {
            if (api.DefaultRequestHeaders.Authorization is not null)
            {
                using var profileResponse = await api.GetAsync("api/auth/me");
                if (profileResponse.StatusCode != HttpStatusCode.Unauthorized)
                {
                    profileResponse.EnsureSuccessStatusCode();
                    var profile = await profileResponse.Content.ReadFromJsonAsync<Profile>();
                    login = profile?.Login;
                    Editor.Visibility = login is null ? Visibility.Collapsed : Visibility.Visible;
                    DeleteButton.Visibility = profile?.Role == "Admin" ? Visibility.Visible : Visibility.Collapsed;
                }
            }
            await LoadAsync();
            var mine = (ReviewsGrid.ItemsSource as List<Review>)?.Find(item => item.Login == login);
            if (mine is not null)
            {
                CommentBox.Text = mine.Comment;
                RatingBox.SelectedIndex = mine.Rating - 1;
            }
        });
    }

    private async Task LoadAsync()
    {
        var reviews = await api.GetFromJsonAsync<ReviewList>($"api/products/{productId}/reviews")
            ?? throw new HttpRequestException("Reviews response is missing.");
        ReviewsGrid.ItemsSource = reviews.Items;
        Heading.Text = reviews.Rating.HasValue ? $"{productName} — {reviews.Rating:N1} из 5" : $"{productName} — пока без оценок";
        StatusText.Text = login is null ? "Войдите в аккаунт, чтобы оставить отзыв." : "Вы можете сохранить или изменить свой отзыв.";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(LoadAsync);

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CommentBox.Text))
        {
            StatusText.Text = "Напишите отзыв.";
            return;
        }
        await RunAsync(async () =>
        {
            using var response = await api.PutAsJsonAsync($"api/products/{productId}/reviews",
                new { rating = RatingBox.SelectedIndex + 1, comment = CommentBox.Text.Trim() });
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
            {
                StatusText.Text = (await response.Content.ReadFromJsonAsync<ApiError>())?.Error ?? "Не удалось сохранить отзыв.";
                return;
            }
            response.EnsureSuccessStatusCode();
            await LoadAsync();
            StatusText.Text = "Ваш отзыв сохранён.";
        });
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (ReviewsGrid.SelectedItem is not Review review)
        {
            StatusText.Text = "Выберите отзыв для удаления.";
            return;
        }
        await RunAsync(async () =>
        {
            using var response = await api.DeleteAsync($"api/admin/products/{productId}/reviews/{review.UserId}");
            response.EnsureSuccessStatusCode();
            await LoadAsync();
            StatusText.Text = "Отзыв удалён.";
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        IsEnabled = false;
        try
        {
            await action();
        }
        catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            StatusText.Text = "Недостаточно прав или сеанс закончился. Войдите снова.";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            StatusText.Text = "Не удалось загрузить отзывы. Проверьте подключение и нажмите «Обновить».";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private sealed record Profile(string Login, string Role);
    private sealed record ReviewList(List<Review> Items, double? Rating);
    private sealed record ApiError(string Error);
    public sealed record Review(int UserId, string Login, int Rating, string Comment);
}
