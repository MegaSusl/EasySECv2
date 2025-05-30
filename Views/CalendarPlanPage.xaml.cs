using EasySECv2.ViewModels;
using Microsoft.Maui.Controls;
using Point = Windows.Foundation.Point;


#if WINDOWS
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.Foundation;
using MenuFlyout = Microsoft.UI.Xaml.Controls.MenuFlyout;
using MenuFlyoutItem = Microsoft.UI.Xaml.Controls.MenuFlyoutItem;
#endif

namespace EasySECv2.Views;

public partial class CalendarPlanPage : ContentPage
{
    private readonly CalendarPlanViewModel _viewModel;

    public CalendarPlanPage(CalendarPlanViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }
    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CalendarPlanViewModel vm)
        {
            vm.RefreshEvents();
        }
    }

    private void OnContextMenuClicked(object sender, EventArgs e)
    {
#if WINDOWS
        if (sender is Microsoft.Maui.Controls.Button btn && btn.CommandParameter is CalendarEvent ev)
        {
            var flyout = new MenuFlyout();

            var editItem = new MenuFlyoutItem { Text = "Изменить" };
            editItem.Click += (_, _) => _viewModel.EditEventCommand.Execute(ev);

            var deleteItem = new MenuFlyoutItem { Text = "Удалить" };
            deleteItem.Click += (_, _) => _viewModel.DeleteEventCommand.Execute(ev);

            flyout.Items.Add(editItem);
            flyout.Items.Add(deleteItem);

            if (btn.Handler?.PlatformView is FrameworkElement nativeButton)
            {
                double offsetX = -54;
                double offsetY = nativeButton.ActualHeight;
                flyout.ShowAt(nativeButton, new Point(offsetX, offsetY));
            }

        }
#endif
    }
}
