using System.Collections.ObjectModel;
using store_app.Models;
using store_app.Services;

namespace store_app;

[QueryProperty(nameof(Category), "category")]
public partial class ProductsPage : ContentPage
{
    const string AllCategory = "Бүх ангилал";

    string selectedCategory = AllCategory;

    public ProductsPage()
    {
        InitializeComponent();
        BindingContext = this;
        RefreshProducts();
    }

    public ObservableCollection<Product> VisibleProducts { get; } = [];

    public IReadOnlyList<CategoryItem> Categories => StoreData.Categories;

    public string Category
    {
        get => selectedCategory;
        set
        {
            selectedCategory = string.IsNullOrWhiteSpace(value)
                ? AllCategory
                : Uri.UnescapeDataString(value);

            RefreshProducts();
        }
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        StoreData.ProductsChanged += OnProductsChanged;
        RefreshProducts();
    }

    protected override void OnDisappearing()
    {
        StoreData.ProductsChanged -= OnProductsChanged;
        base.OnDisappearing();
    }

    async void OnBackClicked(object? sender, EventArgs e)
    {
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
            return;
        }

        await Shell.Current.GoToAsync("//app/home");
    }

    void OnCategoryTapped(object? sender, TappedEventArgs e)
    {
        if ((sender as Element)?.BindingContext is CategoryItem category)
        {
            Category = category.Name;
        }
    }

    async void OnAddClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { BindingContext: Product product })
        {
            return;
        }

        AppState.AddProduct(product);
        await Navigation.PushModalAsync(new CartAddedPage(product), false);
    }

    void OnProductsChanged(object? sender, EventArgs e)
    {
        RefreshProducts();
    }

    void RefreshProducts()
    {
        var products = FilterProducts(selectedCategory).ToList();

        VisibleProducts.Clear();
        foreach (var product in products)
        {
            VisibleProducts.Add(product);
        }

        CategoryTitleLabel.Text = selectedCategory == AllCategory ? "Бүх бараа" : selectedCategory;
        ProductCountLabel.Text = $"{VisibleProducts.Count} бараа";
    }

    static IEnumerable<Product> FilterProducts(string category)
    {
        return category switch
        {
            AllCategory => StoreData.Products,
            "Шинэ бараа" => StoreData.Products.Where(product => product.Badge.Contains("Шинэ", StringComparison.OrdinalIgnoreCase)),
            "Хямдрал" => StoreData.Products.Where(product => product.HasOldPrice || product.Badge.Contains("Хямдрал", StringComparison.OrdinalIgnoreCase) || product.Badge.Contains('-')),
            _ => StoreData.Products.Where(product => product.Category == category),
        };
    }
}
