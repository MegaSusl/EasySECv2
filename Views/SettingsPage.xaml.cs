using EasySECv2.Services;

namespace EasySECv2.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly DatabaseService _db;

        public SettingsPage(DatabaseService dbService)
        {
            InitializeComponent();
            _db = dbService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var tables = await _db.GetAllTableNamesAsync();
            TablesPicker.ItemsSource = tables;
        }

        private async void OnDeleteAllClicked(object sender, EventArgs e)
        {
            var table = TablesPicker.SelectedItem as string;
            if (string.IsNullOrEmpty(table))
            {
                await DisplayAlert("������", "������� �������� �������", "OK");
                return;
            }

            bool confirm = await DisplayAlert(
                "�����������",
                $"������� ��� ������ �� ������� �{table}�?",
                "��", "������");
            if (!confirm) return;

            try
            {
                int deleted = await _db.DeleteAllFromTableAsync(table);
                await DisplayAlert(
                    "������",
                    $"������� �������� {deleted} ����� �� �{table}�.",
                    "OK");
            }
            catch (Exception ex)
            {
                await DisplayAlert("������", ex.Message, "OK");
            }
        }

        private async void OnSeedOrientationsClicked(object sender, EventArgs e)
        {
            await _db.SeedOrientationsIfNeededAsync();
            await DisplayAlert("������", "������� �Orientation� ��������� �� JSON.", "OK");
        }
    }
}
