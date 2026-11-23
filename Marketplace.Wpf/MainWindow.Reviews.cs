using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private async void Reviews_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is not ProductItem product)
        {
            StatusText.Text = "Выберите товар.";
            return;
        }
        new ReviewsWindow(Api, product) { Owner = this }.ShowDialog();
        await LoadCatalogAsync();
    }
}
