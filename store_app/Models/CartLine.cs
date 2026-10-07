using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace store_app.Models;

public sealed class CartLine : INotifyPropertyChanged
{
    int quantity;

    public CartLine(Product product, int quantity)
    {
        Product = product;
        this.quantity = quantity;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public Product Product { get; }

    public int Quantity
    {
        get => quantity;
        set
        {
            if (quantity == value)
            {
                return;
            }

            quantity = Math.Max(1, value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(LineTotal));
            OnPropertyChanged(nameof(LineTotalText));
            OnPropertyChanged(nameof(LinePointsText));
        }
    }

    public decimal LineTotal => Product.Price * Quantity;

    public string LineTotalText => Money.Format(LineTotal);

    public string LinePointsText => $"+{Money.PointsFromPurchase(LineTotal)} оноо";

    void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
