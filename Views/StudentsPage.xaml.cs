using EasySECv2.ViewModels;

namespace EasySECv2.Views
{
    public partial class StudentsPage : ContentPage
    {
        public StudentsPage(StudentsViewModel vm)
        {
            InitializeComponent();
            BindingContext = vm;
        }
        protected override void OnAppearing()
        {
            base.OnAppearing();

            // ��� ������ ������ �������� ��������� RefreshCommand,
            // ����� ��������� ��������� �� ��
            if (BindingContext is StudentsViewModel vm)
                vm.RefreshCommand.Execute(null);
        }
    }
}
