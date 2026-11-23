using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Marketplace.Wpf;

public partial class AdminOrdersWindow : Window
{
    private readonly HttpClient api;
    private int? pointId;
    private static readonly StatusOption[] Statuses =
    [
        new("Placed", "Оформлен"), new("Processing", "В обработке"),
        new("Shipped", "Передан в доставку"), new("Ready", "Готов к выдаче"),
        new("Received", "Получен"), new("Cancelled", "Отменён")
    ];

    public AdminOrdersWindow(HttpClient api)
    {
        InitializeComponent();
        this.api = api;
        StatusBox.ItemsSource = Statuses;
        Loaded += async (_, _) => await RunAsync(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var orders = await api.GetFromJsonAsync<List<AdminOrder>>("api/admin/orders") ?? [];
        var points = await api.GetFromJsonAsync<List<PickupPoint>>("api/admin/pickup-points") ?? [];
        OrdersGrid.ItemsSource = orders;
        PointsGrid.ItemsSource = points;
        LinesGrid.ItemsSource = null;
        DeliveryText.Text = string.Empty;
        StatusBox.SelectedIndex = -1;
        ClearPoint();
        MessageText.Text = $"Заказов: {orders.Count}. ПВЗ: {points.Count}.";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(LoadAsync);

    private void Order_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not AdminOrder order)
            return;
        LinesGrid.ItemsSource = order.Items;
        DeliveryText.Text = order.PickupPointId.HasValue
            ? $"Самовывоз: {order.PickupAddress}" : $"Доставка: {order.DeliveryAddress}";
        StatusBox.SelectedValue = order.Status;
    }

    private async void SaveStatus_Click(object sender, RoutedEventArgs e)
    {
        if (OrdersGrid.SelectedItem is not AdminOrder order || StatusBox.SelectedItem is not StatusOption status)
        {
            MessageText.Text = "Выберите заказ и его статус.";
            return;
        }
        await RunAsync(async () =>
        {
            using var response = await api.PutAsJsonAsync($"api/admin/orders/{order.Id}/status", new { status = status.Code });
            if (await AcceptAsync(response))
            {
                await LoadAsync();
                MessageText.Text = $"Статус заказа №{order.Id} сохранён.";
            }
        });
    }

    private void Point_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (PointsGrid.SelectedItem is not PickupPoint point)
            return;
        pointId = point.Id;
        AddressBox.Text = point.Address;
        ActiveBox.IsChecked = point.Active;
    }

    private void ClearPoint()
    {
        pointId = null;
        AddressBox.Clear();
        ActiveBox.IsChecked = true;
    }

    private void NewPoint_Click(object sender, RoutedEventArgs e)
    {
        PointsGrid.SelectedItem = null;
        ClearPoint();
    }

    private async void SavePoint_Click(object sender, RoutedEventArgs e)
    {
        var address = AddressBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(address))
        {
            MessageText.Text = "Введите адрес ПВЗ.";
            return;
        }
        var input = new { address, isActive = ActiveBox.IsChecked == true ? 1 : 0 };
        await RunAsync(async () =>
        {
            using var response = pointId.HasValue
                ? await api.PutAsJsonAsync($"api/admin/pickup-points/{pointId}", input)
                : await api.PostAsJsonAsync("api/admin/pickup-points", input);
            if (await AcceptAsync(response))
            {
                await LoadAsync();
                MessageText.Text = "ПВЗ сохранён.";
            }
        });
    }

    private async Task<bool> AcceptAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            MessageText.Text = (await response.Content.ReadFromJsonAsync<ApiError>())?.Error ?? "Проверьте введённые данные.";
            return false;
        }
        response.EnsureSuccessStatusCode();
        return true;
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
            MessageText.Text = "Войдите с правами администратора.";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            MessageText.Text = "Не удалось выполнить запрос. Обновите список и проверьте результат перед повтором.";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private sealed record StatusOption(string Code, string Name);
    private sealed record ApiError(string Error);
    private sealed record PickupPoint(int Id, string Address, int IsActive)
    {
        public bool Active => IsActive == 1;
    }
    private sealed record AdminOrder(int Id, string Login, DateTime CreatedAt, string Status, int TotalKopecks,
        int? PickupPointId, string? PickupAddress, string? DeliveryAddress, List<OrderHistoryWindow.OrderLine> Items)
    {
        public DateTime LocalDate => DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc).ToLocalTime();
        public decimal Total => TotalKopecks / 100m;
        public string StatusText => Statuses.FirstOrDefault(status => status.Code == Status)?.Name ?? Status;
    }
}
