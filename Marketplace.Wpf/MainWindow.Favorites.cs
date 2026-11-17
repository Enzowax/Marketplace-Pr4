using System.Net;
using System.Net.Http;
using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private void Favorites_Click(object sender, RoutedEventArgs e)
    {
        if (Api.DefaultRequestHeaders.Authorization is null)
        {
            AccountStatus.Text = "Войдите, чтобы открыть избранное.";
            return;
        }
        new FavoritesWindow(Api) { Owner = this }.ShowDialog();
    }

    private async void AddFavorite_Click(object sender, RoutedEventArgs e)
    {
        if (Api.DefaultRequestHeaders.Authorization is null)
        {
            AccountStatus.Text = "Войдите, чтобы добавить товар в избранное.";
            return;
        }
        if (ProductsGrid.SelectedItem is not ProductItem product)
        {
            StatusText.Text = "Выберите товар.";
            return;
        }

        FavoriteButton.IsEnabled = false;
        try
        {
            using var response = await Api.PutAsync($"api/favorites/{product.Id}", null);
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                StatusText.Text = "Данные изменились. Повторите добавление.";
                return;
            }
            response.EnsureSuccessStatusCode();
            StatusText.Text = $"«{product.Name}» добавлен в избранное.";
        }
        catch (HttpRequestException error) when (error.StatusCode == HttpStatusCode.Unauthorized)
        {
            AccountStatus.Text = "Сеанс закончился. Войдите снова.";
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            StatusText.Text = "Не удалось добавить товар. Проверьте подключение и повторите действие.";
        }
        finally
        {
            FavoriteButton.IsEnabled = true;
        }
    }
}
