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
    }
}
