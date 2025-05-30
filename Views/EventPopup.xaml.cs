using CommunityToolkit.Maui.Views;
using System;
using EasySECv2.ViewModels;

namespace EasySECv2.Views;

public partial class EventPopup : Popup
{
    public EventPopup()
    {
        InitializeComponent();
    }

    private void OnCancelClicked(object sender, EventArgs e)
    {
        Close(null); // Закрыть попап без результата
    }

    private void OnConfirmClicked(object sender, EventArgs e)
    {
        if (BindingContext is CalendarEvent model)
        {
            Close(model); // Возвращаем модель как результат
        }
        else
        {
            Close(null);
        }
    }
}