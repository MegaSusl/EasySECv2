using EasySECv2.Services;
using EasySECv2.Models;
using EasySECv2.ViewModels;

namespace EasySECv2.Views;

public partial class DepartmentPage : ContentPage
{
    public DepartmentPage(DepartmentViewModel vm)
    {
        InitializeComponent();
        BindingContext = vm;
    }
}
