using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace Marketplace.Wpf;

public partial class AdminCatalogWindow : Window
{
    private readonly HttpClient api;
    private int? productId;
    private int? categoryId;

    public AdminCatalogWindow(HttpClient api)
    {
        InitializeComponent();
        this.api = api;
        Loaded += async (_, _) => await RunAsync(LoadAsync);
    }

    private async Task LoadAsync()
    {
        var categories = await api.GetFromJsonAsync<List<Category>>("api/categories") ?? [];
        var products = await api.GetFromJsonAsync<List<Product>>("api/admin/products") ?? [];
        CategoriesList.ItemsSource = categories;
        ProductCategoryBox.ItemsSource = categories;
        ProductsGrid.ItemsSource = products;
        ClearProduct();
        categoryId = null;
        CategoryNameBox.Clear();
        StatusText.Text = "Выберите запись для редактирования или создайте новую.";
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RunAsync(LoadAsync);

    private void NewProduct_Click(object sender, RoutedEventArgs e)
    {
        ProductsGrid.SelectedItem = null;
        ClearProduct();
    }

    private void ClearProduct()
    {
        productId = null;
        NameBox.Clear();
        DescriptionBox.Clear();
        ImageBox.Clear();
        PriceBox.Text = "0";
        StockBox.Text = "0";
        PackBox.Clear();
        AvailableBox.IsChecked = true;
        ProductCategoryBox.SelectedIndex = 0;
    }

    private void Product_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not Product product)
            return;
        productId = product.Id;
        NameBox.Text = product.Name;
        DescriptionBox.Text = product.Description;
        ImageBox.Text = product.ImagePath;
        ProductCategoryBox.SelectedValue = product.CategoryId;
        PriceBox.Text = (product.PriceKopecks / 100m).ToString("0.00");
        StockBox.Text = product.Stock.ToString();
        PackBox.Text = product.PackSize?.ToString() ?? string.Empty;
        AvailableBox.IsChecked = product.IsAvailable == 1;
    }

    private async void SaveProduct_Click(object sender, RoutedEventArgs e)
    {
        if (ProductCategoryBox.SelectedItem is not Category category
            || !decimal.TryParse(PriceBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var price)
            || price < 0 || price > int.MaxValue / 100m || price * 100 != decimal.Truncate(price * 100)
            || !int.TryParse(StockBox.Text, out var stock) || stock < 0)
        {
            StatusText.Text = "Проверьте категорию, цену и остаток.";
            return;
        }
        int? pack = null;
        if (!string.IsNullOrWhiteSpace(PackBox.Text))
        {
            if (!int.TryParse(PackBox.Text, out var quantity) || quantity <= 0)
            {
                StatusText.Text = "Количество в упаковке должно быть положительным целым числом.";
                return;
            }
            pack = quantity;
        }
        var input = new
        {
            categoryId = category.Id, name = NameBox.Text.Trim(), description = DescriptionBox.Text.Trim(),
            imagePath = ImageBox.Text.Trim(), priceKopecks = (int)(price * 100), stock,
            packSize = pack, isAvailable = AvailableBox.IsChecked == true ? 1 : 0
        };
        await RunAsync(async () =>
        {
            using var response = productId.HasValue
                ? await api.PutAsJsonAsync($"api/admin/products/{productId}", input)
                : await api.PostAsJsonAsync("api/admin/products", input);
            if (await AcceptAsync(response))
            {
                await LoadAsync();
                StatusText.Text = "Товар сохранён.";
            }
        });
    }

    private void Category_Selected(object sender, SelectionChangedEventArgs e)
    {
        if (CategoriesList.SelectedItem is Category category)
        {
            categoryId = category.Id;
            CategoryNameBox.Text = category.Name;
        }
    }

    private void NewCategory_Click(object sender, RoutedEventArgs e)
    {
        CategoriesList.SelectedItem = null;
        categoryId = null;
        CategoryNameBox.Clear();
    }

    private async void SaveCategory_Click(object sender, RoutedEventArgs e)
    {
        var name = CategoryNameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            StatusText.Text = "Введите название категории.";
            return;
        }
        await RunAsync(async () =>
        {
            using var response = categoryId.HasValue
                ? await api.PutAsJsonAsync($"api/admin/categories/{categoryId}", new { name })
                : await api.PostAsJsonAsync("api/admin/categories", new { name });
            if (await AcceptAsync(response))
            {
                await LoadAsync();
                StatusText.Text = "Категория сохранена.";
            }
        });
    }

    private async Task<bool> AcceptAsync(HttpResponseMessage response)
    {
        if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Conflict)
        {
            StatusText.Text = (await response.Content.ReadFromJsonAsync<ApiError>())?.Error ?? "Проверьте введённые данные.";
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
        catch (HttpRequestException error) when (error.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized)
        {
            StatusText.Text = "Нужен вход с правами администратора.";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException or System.Text.Json.JsonException)
        {
            StatusText.Text = "Не удалось выполнить запрос. Обновите список и проверьте результат перед повторным сохранением.";
        }
        finally
        {
            IsEnabled = true;
        }
    }

    private sealed record Category(int Id, string Name);
    private sealed record ApiError(string Error);
    private sealed record Product(int Id, int CategoryId, string Name, string Description, string ImagePath,
        int PriceKopecks, int Stock, int? PackSize, int IsAvailable);
}
