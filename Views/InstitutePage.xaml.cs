using EasySECv2.ViewModels;

namespace EasySECv2.Views
{
    public partial class InstitutePage : ContentPage
    {
        public InstitutePage(InstituteViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
    }
}
