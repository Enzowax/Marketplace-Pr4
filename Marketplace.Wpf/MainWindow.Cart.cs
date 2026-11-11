using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private void Cart_Click(object sender, RoutedEventArgs e) => OpenCart(null);

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is ProductItem { CanBuy: true } product)
            OpenCart(product);
    }

    private void OpenCart(ProductItem? product)
    {
        if (Api.DefaultRequestHeaders.Authorization is null)
        {
            AccountStatus.Text = "Войдите, чтобы пользоваться корзиной.";
            LoginBox.Focus();
            return;
        }

        new CartWindow(Api, product) { Owner = this }.ShowDialog();
    }
}
