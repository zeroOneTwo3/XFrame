using XFrame.Models;
using XFrame.PageModels;

namespace XFrame.Pages;

public partial class MainPage : ContentPage
{
    public MainPage(MainPageModel model)
    {
        InitializeComponent();
        BindingContext = model;
    }
}