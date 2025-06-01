using EasySECv2.ViewModels;

namespace EasySECv2.Views;

public partial class FormPage : ContentPage
{
	public FormPage()
	{
		InitializeComponent();
	}
    private void OnAddMemberClicked(object sender, EventArgs e)
    {
        if (BindingContext is FormViewModel vm)
        {
            var field = vm.Fields.FirstOrDefault(f => f.IsMemberAndSecretarySelector);
            if (field != null && field.MemberPickers.Count < 4)
            {
                field.MemberPickers.Add(new FormViewModel.MemberSelection
                {
                    SelectedStaffId = field.AllStaff.FirstOrDefault()?.Id ?? 0,
                    StaffOptions = field.AllStaff
                });
            }
        }
    }


}