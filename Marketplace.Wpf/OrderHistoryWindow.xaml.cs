using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Marketplace.Wpf;

public partial class OrderHistoryWindow : Window
{
    private readonly HttpClient api;

    public OrderHistoryWindow(HttpClient api)
    {
        InitializeComponent();
        this.api = api;
        Loaded += async (_, _) => await LoadAsync();
        SortBox.SelectionChanged += async (_, _) => await LoadAsync();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private async Task LoadAsync()
    {
        await RunAsync(async () =>
        {
            OrdersGrid.ItemsSource = null;
            ItemsGrid.ItemsSource = null;
            DeliveryText.Text = string.Empty;
            var orders = await api.GetFromJsonAsync<List<OrderView>>(
                $"api/orders?oldestFirst={(SortBox.SelectedIndex == 1 ? "true" : "false")}") ?? [];
            OrdersGrid.ItemsSource = orders;
            StatusText.Text = orders.Count == 0 ? "У вас пока нет заказов." : $"Заказов: {orders.Count}. Выберите заказ.";
        });
    }

    private async void Order_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not OrderView selected)
            return;
        await RunAsync(async () =>
        {
            ItemsGrid.ItemsSource = null;
            DeliveryText.Text = string.Empty;
            var order = await api.GetFromJsonAsync<OrderView>($"api/orders/{selected.Id}")
                ?? throw new HttpRequestException("Order response is missing.");
            ItemsGrid.ItemsSource = order.Items;
            DeliveryText.Text = order.PickupPointId.HasValue
                ? $"Самовывоз: {order.PickupAddress}"
                : $"Доставка: {order.DeliveryAddress}";
            StatusText.Text = $"Заказ №{order.Id}. {order.StatusText}. Итого: {order.Total:N2} ₽";
        });
    }

    private async Task RunAsync(Func<Task> action)
    {
        IsEnabled = false;
        StatusText.Text = "Загрузка…";
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
            StatusText.Text = "Не удалось загрузить заказы. Проверьте подключение и нажмите «Обновить».";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    public sealed record OrderView(int Id, DateTime CreatedAt, string Status, int TotalKopecks,
        int? PickupPointId, string? PickupAddress, string? DeliveryAddress, List<OrderLine> Items)
    {
        public DateTime LocalDate => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();
        public decimal Total => TotalKopecks / 100m;
        public string StatusText => Status switch
        {
            "Placed" => "Оформлен",
            "Processing" => "В обработке",
            "Shipped" => "Передан в доставку",
            "Ready" => "Готов к выдаче",
            "Received" => "Получен",
            "Cancelled" => "Отменён",
            _ => Status
        };
    }

    public sealed record OrderLine(string Name, int Quantity, int UnitPriceKopecks)
    {
        public decimal Price => UnitPriceKopecks / 100m;
        public decimal Total => Price * Quantity;
    }
}
