using EasySECv2.ViewModels;

namespace EasySECv2.Views
{
    public partial class OrientationPage : ContentPage
    {
        public OrientationPage(OrientationViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as OrientationViewModel)?.RefreshCommand.Execute(null);
        }
    }
}
