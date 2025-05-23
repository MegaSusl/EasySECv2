using EasySECv2.ViewModels;

namespace EasySECv2.Views;

public partial class SecCompositionPage : ContentPage
{
    public SecCompositionPage(SecCompositionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}