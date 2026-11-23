using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private void AdminOrders_Click(object sender, RoutedEventArgs e)
    {
        new AdminOrdersWindow(Api) { Owner = this }.ShowDialog();
    }

    private async void AdminCatalog_Click(object sender, RoutedEventArgs e)
    {
        new AdminCatalogWindow(Api) { Owner = this }.ShowDialog();
        CategoryBox.ItemsSource = null;
        await LoadCatalogAsync();
    }
}
