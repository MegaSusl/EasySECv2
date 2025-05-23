using EasySECv2.ViewModels;

namespace EasySECv2.Views
{
    public partial class StaffPage : ContentPage
    {
        public StaffPage(StaffViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ��� ������ ������ �������� ��������� RefreshCommand,
            // ����� ��������� ��������� �� ��
            if (BindingContext is StaffViewModel vm)
                vm.RefreshCommand.Execute(null);
        }
    }
}
