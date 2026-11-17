using System.Windows;

namespace Marketplace.Wpf;

public partial class MainWindow
{
    private void OrderHistory_Click(object sender, RoutedEventArgs e)
    {
        if (Api.DefaultRequestHeaders.Authorization is null)
        {
            AccountStatus.Text = "Войдите, чтобы посмотреть заказы.";
            return;
        }
        new OrderHistoryWindow(Api) { Owner = this }.ShowDialog();
    }

    private void Cart_Click(object sender, RoutedEventArgs e) => OpenCart(null);

    private void AddToCart_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is ProductItem { CanBuy: true } product)
            OpenCart(product);
    }

    private async void BuyNow_Click(object sender, RoutedEventArgs e)
    {
        if (Api.DefaultRequestHeaders.Authorization is null)
        {
            AccountStatus.Text = "Войдите, чтобы оформить заказ.";
            return;
        }
        if (ProductsGrid.SelectedItem is ProductItem { CanBuy: true } product
            && new CheckoutWindow(Api, product) { Owner = this }.ShowDialog() == true)
            await LoadCatalogAsync();
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
