using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Marketplace.Wpf;

public partial class CheckoutWindow : Window
{
    private readonly HttpClient api;
    private readonly int? productId;

    public CheckoutWindow(HttpClient api, MainWindow.ProductItem? product = null)
    {
        InitializeComponent();
        this.api = api;
        productId = product?.Id;
        Summary.Text = product is null ? "Оформление всей корзины" : $"Купить: {product.Name} — 1 шт.";
        Loaded += async (_, _) =>
        {
            SubmitButton.IsEnabled = false;
            try
            {
                PickupBox.ItemsSource = await api.GetFromJsonAsync<List<PickupPoint>>("api/pickup-points");
                PickupBox.SelectedIndex = 0;
                decimal? total = product?.PriceKopecks;
                if (total is null)
                {
                    var cart = await api.GetFromJsonAsync<CartTotal>("api/cart");
                    total = cart?.TotalKopecks;
                }
                DeliveryOption.IsEnabled = total > 60000;
                Summary.Text += $"\nСумма: {total / 100m:N2} ₽";
                SubmitButton.IsEnabled = total > 0;
            }
            catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
            {
                StatusText.Text = "Не удалось загрузить данные. Закройте окно и повторите попытку.";
            }
        };
    }

    private async void Submit_Click(object sender, RoutedEventArgs e)
    {
        var delivery = DeliveryOption.IsChecked == true;
        var point = PickupBox.SelectedItem as PickupPoint;
        if (delivery ? string.IsNullOrWhiteSpace(AddressBox.Text) : point is null)
        {
            StatusText.Text = "Укажите адрес доставки или выберите ПВЗ.";
            return;
        }

        SubmitButton.IsEnabled = false;
        try
        {
            using var response = await api.PostAsJsonAsync("api/orders", new
            {
                productId, quantity = 1,
                pickupPointId = delivery ? (int?)null : point!.Id,
                deliveryAddress = delivery ? AddressBox.Text.Trim() : null
            });
            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    StatusText.Text = "Сеанс закончился. Войдите снова.";
                else
                    StatusText.Text = (await response.Content.ReadFromJsonAsync<ApiError>())?.Error ?? "Заказ не оформлен.";
                SubmitButton.IsEnabled = true;
                return;
            }
            var order = await response.Content.ReadFromJsonAsync<PlacedOrder>();
            MessageBox.Show(this, $"Заказ №{order?.Id} оформлен.", "Заказ");
            DialogResult = true;
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            StatusText.Text = "Ответ не получен. Проверьте историю заказов перед повторным оформлением.";
        }
    }

    private sealed record PickupPoint(int Id, string Address);
    private sealed record CartTotal(decimal TotalKopecks);
    private sealed record PlacedOrder(int Id);
    private sealed record ApiError(string Error);
}
