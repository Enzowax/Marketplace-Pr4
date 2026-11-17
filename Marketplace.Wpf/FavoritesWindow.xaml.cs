using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Marketplace.Wpf;

public partial class FavoritesWindow : Window
{
    private readonly HttpClient api;

    public FavoritesWindow(HttpClient api)
    {
        InitializeComponent();
        this.api = api;
        Loaded += async (_, _) => await RunAsync(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var items = await api.GetFromJsonAsync<List<Favorite>>("api/favorites") ?? [];
        FavoritesGrid.ItemsSource = items;
        StatusText.Text = items.Count == 0 ? "Избранное пока пусто." : $"Товаров в избранном: {items.Count}";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(LoadAsync);

    private async void Move_Click(object sender, RoutedEventArgs e) => await ChangeAsync(toCart: true);

    private async void Remove_Click(object sender, RoutedEventArgs e) => await ChangeAsync(toCart: false);

    private async Task ChangeAsync(bool toCart)
    {
        if (FavoritesGrid.SelectedItem is not Favorite item)
        {
            StatusText.Text = "Выберите товар.";
            return;
        }

        await RunAsync(async () =>
        {
            using var response = toCart
                ? await api.PostAsync($"api/favorites/{item.Id}/cart", null)
                : await api.DeleteAsync($"api/favorites/{item.Id}");
            if (response.StatusCode is HttpStatusCode.Conflict or HttpStatusCode.NotFound)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                await LoadAsync();
                StatusText.Text = error?.Error ?? "Обновите список и повторите действие.";
                return;
            }
            response.EnsureSuccessStatusCode();
            await LoadAsync();
            if (toCart)
                StatusText.Text = "Товар перенесён в корзину. Количество увеличено на одну единицу.";
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        IsEnabled = false;
        try
        {
            await action();
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            StatusText.Text = "Сеанс закончился. Войдите снова.";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            StatusText.Text = "Не удалось обновить избранное. Проверьте подключение и нажмите «Обновить».";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    public sealed record Favorite(int Id, string Name, int PriceKopecks, int Stock, bool CanBuy)
    {
        public decimal Price => PriceKopecks / 100m;
    }
    private sealed record ApiError(string Error);
}
