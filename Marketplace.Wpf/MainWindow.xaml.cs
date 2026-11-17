using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow : Window
{
    private static readonly HttpClient Api = new()
    {
        BaseAddress = new Uri("http://localhost:5124/"),
        Timeout = TimeSpan.FromSeconds(10)
    };

    public MainWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) => await LoadCatalogAsync();
    }

    private async void Search_Click(object sender, RoutedEventArgs e) => await LoadCatalogAsync();

    private async Task LoadCatalogAsync()
    {
        if (!TryReadPrice(MinPriceBox.Text, out var min) || !TryReadPrice(MaxPriceBox.Text, out var max)
            || min > max)
        {
            StatusText.Text = "Укажите корректный диапазон цен в рублях.";
            return;
        }

        SearchButton.IsEnabled = false;
        StatusText.Text = "Загрузка каталога…";
        try
        {
            if (CategoryBox.ItemsSource is null)
            {
                var categories = await Api.GetFromJsonAsync<List<CategoryItem>>("api/categories") ?? [];
                categories.Insert(0, new CategoryItem(0, "Все категории"));
                CategoryBox.ItemsSource = categories;
                CategoryBox.SelectedIndex = 0;
            }

            var filters = new List<string> { $"search={Uri.EscapeDataString(SearchBox.Text.Trim())}" };
            if (CategoryBox.SelectedItem is CategoryItem { Id: > 0 } category)
                filters.Add($"categoryId={category.Id}");
            if (min.HasValue)
                filters.Add($"minPriceKopecks={min.Value}");
            if (max.HasValue)
                filters.Add($"maxPriceKopecks={max.Value}");

            var products = await Api.GetFromJsonAsync<List<ProductItem>>("api/products?" + string.Join("&", filters)) ?? [];
            ProductsGrid.ItemsSource = products;
            StatusText.Text = products.Count == 0 ? "Товары не найдены." : $"Найдено товаров: {products.Count}";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            StatusText.Text = "Не удалось загрузить каталог. Проверьте, запущен ли API.";
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private static bool TryReadPrice(string text, out int? kopecks)
    {
        kopecks = null;
        if (string.IsNullOrWhiteSpace(text))
            return true;
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingWhite
            | NumberStyles.AllowTrailingWhite, CultureInfo.CurrentCulture, out var rubles)
            || rubles < 0 || rubles > int.MaxValue / 100m)
            return false;

        var amount = rubles * 100;
        if (amount != decimal.Truncate(amount))
            return false;
        kopecks = (int)amount;
        return true;
    }

    public sealed record CategoryItem(int Id, string Name);

    public sealed record ProductItem(int Id, string Name, string Description, string Category,
        int PriceKopecks, int Stock, int? PackSize, bool CanBuy, double? Rating)
    {
        public decimal Price => PriceKopecks / 100m;
    }
}
