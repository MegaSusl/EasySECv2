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

            // При каждом показе страницы выполняем RefreshCommand,
            // чтобы подтянуть изменения из БД
            if (BindingContext is StudentsViewModel vm)
                vm.RefreshCommand.Execute(null);
        }
    }
}
