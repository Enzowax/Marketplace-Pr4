using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Marketplace.Wpf;

public partial class CartWindow : Window
{
    private readonly HttpClient api;
    private readonly MainWindow.ProductItem? initialProduct;
    private int? selectedId;

    public CartWindow(HttpClient api, MainWindow.ProductItem? product)
    {
        InitializeComponent();
        this.api = api;
        initialProduct = product;
        Loaded += async (_, _) => await RunAsync(() => LoadAsync(initialProduct?.Id));
    }

    private async Task LoadAsync(int? productId)
    {
        var cart = await api.GetFromJsonAsync<CartView>("api/cart")
            ?? throw new HttpRequestException("API did not return a cart.");
        ItemsGrid.ItemsSource = cart.Items;
        TotalText.Text = $"Итого: {cart.TotalKopecks / 100m:N2} ₽";
        var item = cart.Items.Find(row => row.ProductId == productId);
        ItemsGrid.SelectedItem = item;
        if (item is null && initialProduct is not null && initialProduct.Id == productId)
            Select(productId, initialProduct.Name, 1, canRemove: false);
        else
            ShowSelection();
    }

    private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e) => ShowSelection();

    private void ShowSelection()
    {
        var item = ItemsGrid.SelectedItem as CartRow;
        Select(item?.ProductId, item?.Name ?? string.Empty, item?.Quantity ?? 1, item is not null);
    }

    private void Select(int? id, string name, int quantity, bool canRemove)
    {
        selectedId = id;
        SelectedName.Text = name;
        QuantityBox.Text = id.HasValue ? quantity.ToString() : string.Empty;
        Actions.IsEnabled = id.HasValue;
        RemoveButton.IsEnabled = canRemove;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (selectedId is not int id)
            return;
        if (!int.TryParse(QuantityBox.Text, out var quantity) || quantity <= 0)
        {
            StatusText.Text = "Введите целое количество больше нуля.";
            return;
        }

        await RunAsync(async () =>
        {
            using var response = await api.PutAsJsonAsync($"api/cart/{id}", new { quantity });
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                var error = await response.Content.ReadFromJsonAsync<ApiError>();
                StatusText.Text = error?.Error ?? "Не удалось обновить количество.";
                return;
            }
            response.EnsureSuccessStatusCode();
            await LoadAsync(id);
        });
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (selectedId is not int id)
            return;
        await RunAsync(async () =>
        {
            using var response = await api.DeleteAsync($"api/cart/{id}");
            response.EnsureSuccessStatusCode();
            await LoadAsync(null);
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        IsEnabled = false;
        StatusText.Text = string.Empty;
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
            StatusText.Text = "Не удалось обновить корзину. Проверьте подключение к API.";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private sealed record CartView(List<CartRow> Items, decimal TotalKopecks);
    private sealed record ApiError(string Error);
    public sealed record CartRow(int ProductId, string Name, int Quantity, int PriceKopecks, bool CanBuy)
    {
        public decimal Price => PriceKopecks / 100m;
        public decimal Total => Price * Quantity;
    }
}
