// FqwEditPage.xaml.cs
using EasySECv2.ViewModels;

namespace EasySECv2.Views;

[QueryProperty(nameof(StudentIdString), "studentId")]
public partial class FqwEditPage : GenericEditPage
{
    public FqwEditPage(FqwEditViewModel vm) : base(vm) { }

    // сюда приходит строка из URI (?studentId=Е)
    public string StudentIdString
    {
        set
        {
            if (long.TryParse(value, out var id))
                ((FqwEditViewModel)BindingContext).StudentId = id;
        }
    }
}
