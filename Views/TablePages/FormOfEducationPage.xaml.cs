using EasySECv2.ViewModels;
using Microsoft.Maui.Controls;

namespace EasySECv2.Views
{
    public partial class FormOfEducationPage : ContentPage
    {
        public FormOfEducationPage(FormOfEducationViewModel vm)
        {
            try
            {
                InitializeComponent();
                BindingContext = vm;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Ошибка при загрузке FormOfEducationPage: " + ex);
                throw; // можем перекинуть, чтобы отладчик снова остановился
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as FormOfEducationViewModel)?.RefreshCommand.Execute(null);
        }

    }
}
