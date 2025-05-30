using CommunityToolkit.Maui.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasySECv2.Views;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
#if WINDOWS
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;
using static Microsoft.IO.RecyclableMemoryStreamManager;
#endif

namespace EasySECv2.ViewModels;

public partial class CalendarPlanViewModel : ObservableObject
{
    private readonly string filePath;

    [ObservableProperty]
    private ObservableCollection<CalendarEvent> events = new();

    public CalendarPlanViewModel()
    {
        string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string appFolder = Path.Combine(documents, "EasySEC");
        filePath = Path.Combine(appFolder, "calendar_events.json");

        EnsureEventFileExists(appFolder, filePath);
        LoadEventsFromFile(filePath);

        CheckForUpcomingEvents();
        StartClock();
    }

    public void RefreshEvents()
    {
        LoadEventsFromFile(filePath);
    }

    private void EnsureEventFileExists(string folder, string path)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        if (!File.Exists(path))
        {
            var emptyList = new List<CalendarEvent>();
            var json = JsonSerializer.Serialize(emptyList, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
    }

    private void LoadEventsFromFile(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var loaded = JsonSerializer.Deserialize<List<CalendarEvent>>(json);
            if (loaded != null)
            {
                Events = new ObservableCollection<CalendarEvent>(loaded);
                FinalizeChanges();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Ошибка загрузки событий: " + ex.Message);
        }
    }

    [RelayCommand]
    private void DeleteEvent(CalendarEvent ev)
    {
        if (Events.Contains(ev))
        {
            Events.Remove(ev);
            FinalizeChanges();
        }
    }

    [RelayCommand]
    private async Task AddEvent()
    {
        await ShowPopupAsync();
    }

    [RelayCommand]
    private async Task EditEvent(CalendarEvent ev)
    {
        await ShowPopupAsync(ev);
    }

    public async Task ShowPopupAsync(CalendarEvent? existing = null)
    {
        var isEdit = existing is not null;

        var model = isEdit
            ? new CalendarEvent
            {
                Title = existing!.Title,
                Deadline = existing.Deadline,
                Description = existing.Description
            }
            : new CalendarEvent
            {
                Title = string.Empty,
                Deadline = DateTime.Today,
                Description = string.Empty
            };

        model.ActionButtonText = isEdit ? "Редактировать" : "Добавить";
        model.HeaderText = isEdit ? "Изменить событие" : "Создать событие";

        var popup = new EventPopup
        {
            BindingContext = model
        };

        var result = await App.Current.MainPage.ShowPopupAsync(popup);

        if (result is CalendarEvent resultEvent)
        {
            if (isEdit && existing is not null)
            {
                var index = Events.IndexOf(existing);
                if (index >= 0)
                {
                    Events[index] = new CalendarEvent
                    {
                        Title = resultEvent.Title,
                        Deadline = resultEvent.Deadline,
                        Description = resultEvent.Description
                    };
                }
            }
            else
            {
                Events.Add(resultEvent);
            }

            FinalizeChanges();
        }
    }

    private void FinalizeChanges()
    {
        var sorted = Events.OrderBy(e => e.Deadline).ToList();

        foreach (var e in sorted)
            e.IsNearest = false;

        var nearest = sorted
            .Where(e => e.Deadline.Date >= DateTime.Today)
            .FirstOrDefault();

        if (nearest is not null)
            nearest.IsNearest = true;

        for (int i = 0; i < sorted.Count; i++)
        {
            if (i < Events.Count)
                Events[i] = sorted[i];
            else
                Events.Add(sorted[i]);
        }

        while (Events.Count > sorted.Count)
            Events.RemoveAt(Events.Count - 1);


        var json = JsonSerializer.Serialize(Events, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public void CheckForUpcomingEvents()
    {
#if WINDOWS
        foreach (var ev in Events)
        {
            var daysLeft = (ev.Deadline - DateTime.Today).Days;

            if (daysLeft is 0 or 1)
            {
                string title = "Напоминание о событии";
                string content = $"{ev.Title} — срок {ev.Deadline:dd.MM.yyyy}";

                var toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);
                var stringElements = toastXml.GetElementsByTagName("text");

                stringElements[0].AppendChild(toastXml.CreateTextNode(title));
                stringElements[1].AppendChild(toastXml.CreateTextNode(content));

                var toast = new ToastNotification(toastXml);
                ToastNotificationManager.CreateToastNotifier("EasySEC").Show(toast);
            }
        }
#endif
    }

    [ObservableProperty]
    private string currentTime = DateTime.Now.ToString("HH:mm:ss");

    private void StartClock()
    {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (_, _) =>
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            });
        };
        timer.Start();
    }
}

public partial class CalendarEvent : ObservableObject
{
    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private DateTime deadline;

    [ObservableProperty]
    private string? description;

    [JsonIgnore]
    public string ActionButtonText { get; set; } = "Добавить";

    [JsonIgnore]
    public string HeaderText { get; set; } = "Создать событие";

    [JsonIgnore]
    public bool IsNearest { get; set; } = false;

    [JsonIgnore]
    public string TitleWithTag => IsNearest ? $"{Title}  [Ближайшее]" : Title;

    [JsonIgnore]
    public FormattedString TitleFormatted
    {
        get
        {
            var fs = new FormattedString();
            fs.Spans.Add(new Span { Text = Title });
            if (IsNearest)
            {
                fs.Spans.Add(new Span
                {
                    Text = "  [Ближайшее]",
                    TextColor = Application.Current.Resources["Primary"] as Color ?? Colors.Red,
                    FontAttributes = FontAttributes.Bold
                });
            }
            return fs;
        }
    }

    public string DaysLeftDisplay => (Deadline - DateTime.Today).Days switch
    {
        < 0 => $"Срок просрочен на {Math.Abs((Deadline - DateTime.Today).Days)} дн.",
        0 => "Крайний срок сегодня!",
        _ => $"Осталось {(Deadline - DateTime.Today).Days} дн."
    };

    public string? Regulation => Description;
}
