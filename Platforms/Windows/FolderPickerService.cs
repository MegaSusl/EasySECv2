#if WINDOWS
using System.Threading.Tasks;
using EasySECv2.Services;
using Windows.Storage.Pickers;
using WinRT.Interop;

// псевдоним для WinUI-окна
using WinUIWindow = Microsoft.UI.Xaml.Window;

namespace EasySECv2.Services
{
    public class FolderPickerService : IFolderPickerService
    {
        public async Task<string?> PickFolderAsync()
        {
            var picker = new FolderPicker();

            // получаем MAUI-окно из списка
            var mauiWindow = Microsoft.Maui.Controls.Application.Current
                                .Windows[0]
                                .Handler
                                .PlatformView as WinUIWindow;

            // хэндл окна
            var hwnd = WindowNative.GetWindowHandle(mauiWindow);

            InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            var folder = await picker.PickSingleFolderAsync();
            return folder?.Path;
        }
    }
}
#endif
