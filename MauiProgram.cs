// MauiProgram.cs
using System;
using System.IO;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Extensions.DependencyInjection;
using EasySECv2.Services;
using EasySECv2.Views;
using EasySECv2.ViewModels;
using EasySECv2.Models;
using SQLite;
using CommunityToolkit.Maui;
using EasySECv2.WinUI;
using DateTimePicker.MAUI;
using Orientation = EasySECv2.Models.Orientation;
using Microsoft.Maui.Handlers;
using SQLitePCL;
using System.Diagnostics;

#if WINDOWS
using Microsoft.UI.Xaml.Controls;
#endif

namespace EasySECv2
{
    public static class MauiProgram
    {
        public static IServiceProvider Services { get; private set; }
        static MauiProgram()
        {
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "EasySEC");
            Directory.CreateDirectory(logDir);
            var logFile = Path.Combine(logDir, "fatal.log");

#if WINDOWS
            // UI-ошибки только под Windows
            Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
            {
                File.AppendAllText(logFile, $"[WinUI] {e.Exception}\n");
                // e.Handled = true;  // если не хотите «красного» крэша
            };
#endif

            // все остальные
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                File.AppendAllText(logFile, $"[AppDomain] {e.ExceptionObject}\n");

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                File.AppendAllText(logFile, $"[TaskScheduler] {e.Exception}\n");
                e.SetObserved();
            };
        }



        public static MauiApp CreateMauiApp()
        {
            // добавляем TextWriterTraceListener -> лог в той же папке, что и БД
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var logDir = Path.Combine(docs, "EasySEC");
            Directory.CreateDirectory(logDir);
            Trace.Listeners.Add(new TextWriterTraceListener(
                Path.Combine(logDir, "easysec.log")));
            Trace.AutoFlush = true;

            Batteries_V2.Init();
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options => options.SetShouldEnableSnackbarOnWindows(true))
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if WINDOWS
                    handlers.AddHandler<Entry, EntryHandler>();
                    //EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                    //{
                    //    if (handler.PlatformView is TextBox textBox)
                    //    {
                    //        textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                    //        textBox.Background = null;
                    //        textBox.UseSystemFocusVisuals = false;
                    //    }
                    //});
#endif
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
            //
            // 1) Основной сервис базы
            //
            builder.Services.AddSingleton<DatabaseService>(sp =>
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var folder = Path.Combine(docs, "EasySEC");
                Directory.CreateDirectory(folder);
                var dbPath = Path.Combine(folder, "EasySEC.db3");
                var templatesFolder = Path.Combine(folder, "Templates");
                Directory.CreateDirectory(templatesFolder);
                return new DatabaseService(dbPath);
            });
            

            //
            // 2) Помощник для Excel-импорта
            //
            builder.Services.AddSingleton<ExcelAdapter>();

            //
            // 3) CRUD-сервисы для каждой сущности
            //
            builder.Services.AddSingleton<ICrudService<Student>>(sp =>
                new CrudService<Student>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Student>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Department>>(sp =>
                new CrudService<Department>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Department>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Orientation>>(sp =>
                new CrudService<Orientation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Orientation>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Institute>>(sp =>
                new CrudService<Institute>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Institute>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<FormOfEducation>>(sp =>
                new CrudService<FormOfEducation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<FormOfEducation>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Staff>>(sp =>
                new CrudService<Staff>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Staff>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Room>>(sp =>
                new CrudService<Room>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Room>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<FinalQualifyingWork>>(sp =>
                new CrudService<FinalQualifyingWork>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<FinalQualifyingWork>(),
                    (db, it) => it.Id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));

            //
            // 4) «Страницы-списки» и их VM
            //
            builder.Services.AddTransient<StudentsViewModel>();
            builder.Services.AddTransient<StudentsPage>();

            builder.Services.AddTransient<DepartmentViewModel>();
            builder.Services.AddTransient<DepartmentPage>();

            builder.Services.AddTransient<OrientationViewModel>();
            builder.Services.AddTransient<OrientationPage>();

            builder.Services.AddTransient<InstituteViewModel>();
            builder.Services.AddTransient<InstitutePage>();

            builder.Services.AddTransient<FormOfEducationViewModel>();
            builder.Services.AddTransient<FormOfEducationPage>();

            builder.Services.AddTransient<StaffViewModel>();
            builder.Services.AddTransient<StaffPage>();
            //
            // 5) Универсальный редактор: открытый GenericEditViewModel<T>
            //
            //builder.Services.AddTransient(typeof(GenericEditViewModel<>));
            builder.Services.AddTransient(typeof(GenericEditViewModel<>), typeof(GenericEditViewModel<>));

            //
            // 6) «Обёртки» для GenericEditPage — по одной на каждую модель.
            //    Конструктор каждой обёртки принимает соответствующий GenericEditViewModel<T>.
            //
            builder.Services.AddTransient<EditStudentPage>();
            builder.Services.AddTransient<EditDepartmentPage>();
            builder.Services.AddTransient<EditOrientationPage>();
            builder.Services.AddTransient<EditInstitutePage>();
            builder.Services.AddTransient<EditFormOfEducationPage>();
            builder.Services.AddTransient<EditStaffPage>();

            builder.Services.AddTransient<FqwEditPage>();
            builder.Services.AddTransient<FqwEditViewModel>();
            
            //
            // 7) Роуты для навигации
            //
            Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
            Routing.RegisterRoute(nameof(EditDepartmentPage), typeof(EditDepartmentPage));
            Routing.RegisterRoute(nameof(EditOrientationPage), typeof(EditOrientationPage));
            Routing.RegisterRoute(nameof(EditInstitutePage), typeof(EditInstitutePage));
            Routing.RegisterRoute(nameof(EditFormOfEducationPage), typeof(EditFormOfEducationPage));
            Routing.RegisterRoute(nameof(EditStaffPage), typeof(EditStaffPage));
            Routing.RegisterRoute(nameof(FqwEditPage), typeof(FqwEditPage));
            //Routing.RegisterRoute(nameof(GenericEditPage), typeof(GenericEditPage));

            //builder.Services.AddTransient(
            //typeof(GenericEditViewModel<>),
            //serviceProvider =>
            //{
            //    // поставим фабрику, чтобы передавать dbService
            //    return (Type genericType) =>
            //    {
            //        var db = serviceProvider.GetRequiredService<DatabaseService>();
            //        var crudFactory = serviceProvider.GetRequiredService(typeof(ICrudService<>).MakeGenericType(genericType));

            //        // создаём экземпляр через Activator
            //        return Activator.CreateInstance(
            //            typeof(GenericEditViewModel<>).MakeGenericType(genericType),
            //            crudFactory, db);
            //    };
            //});


            builder.Services.AddTransient<SecCompositionViewModel>();
            builder.Services.AddTransient<SecCompositionPage>();
            
            builder.Services.AddTransient<SecSecretaryReplacementViewModel>();
            builder.Services.AddTransient<SecSecretaryReplacementPage>();
            
            builder.Services.AddTransient<SecMemberReplacementViewModel>();
            builder.Services.AddTransient<SecMemberReplacementPage>();

            // Сначала регистрируем парсер
            // А затем сервис, который его использует
            builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
            builder.Services.AddSingleton<IPageSettingsService, PageSettingsService>();
            builder.Services.AddSingleton<IDocumentGenerationService, DocumentGenerationService>();
            builder.Services.AddSingleton<ITemplateService, TemplateService>();

            builder.Services.AddTransient<ProtocolViewModel>();
            builder.Services.AddTransient<ProtocolPage>();

            builder.Services.AddSingleton<FamiliarizationViewModel>();
            builder.Services.AddSingleton<FamiliarizationPage>();
            
            builder.Services.AddSingleton<CalendarPlanViewModel>();
            builder.Services.AddSingleton<CalendarPlanPage>();

            builder.Services.AddSingleton<VkrInventoryViewModel>();
            builder.Services.AddSingleton<VkrInventoryPage>();

            builder.Services.AddSingleton<StateExamScheduleViewModel>();
            builder.Services.AddSingleton<StateExamSchedulePage>();

            builder.Services.AddSingleton<StateExamScheduleUmuViewModel>();
            builder.Services.AddSingleton<StateExamScheduleUmuPage>();

            builder.Services.AddSingleton<VkrTopicAssignmentViewModel>();
            builder.Services.AddSingleton<VkrTopicAssignmentPage>();

            var app = builder.Build();

            Services = app.Services;

            return app;
        }
        public static T GetService<T>() where T : notnull => Services.GetRequiredService<T>();
    }
}
