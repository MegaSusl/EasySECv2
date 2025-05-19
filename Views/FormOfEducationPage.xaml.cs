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
                System.Diagnostics.Debug.WriteLine("������ ��� �������� FormOfEducationPage: " + ex);
                throw; // ����� ����������, ����� �������� ����� �����������
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as FormOfEducationViewModel)?.RefreshCommand.Execute(null);
        }

    }
}
