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

namespace EasySECv2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit(options => options.SetShouldEnableSnackbarOnWindows(true))
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
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
            builder.Services.AddSingleton<ITemplateService, TemplateService>();

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
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Department>>(sp =>
                new CrudService<Department>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Department>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Orientation>>(sp =>
                new CrudService<Orientation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Orientation>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Institute>>(sp =>
                new CrudService<Institute>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Institute>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<FormOfEducation>>(sp =>
                new CrudService<FormOfEducation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<FormOfEducation>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                ));
            builder.Services.AddSingleton<ICrudService<Staff>>(sp =>
                new CrudService<Staff>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Staff>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
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
            builder.Services.AddTransient(typeof(GenericEditViewModel<>));

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

            //
            // 7) Роуты для навигации
            //
            Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
            Routing.RegisterRoute(nameof(EditDepartmentPage), typeof(EditDepartmentPage));
            Routing.RegisterRoute(nameof(EditOrientationPage), typeof(EditOrientationPage));
            Routing.RegisterRoute(nameof(EditInstitutePage), typeof(EditInstitutePage));
            Routing.RegisterRoute(nameof(EditFormOfEducationPage), typeof(EditFormOfEducationPage));
            Routing.RegisterRoute(nameof(EditStaffPage), typeof(EditStaffPage));
            //Routing.RegisterRoute(nameof(GenericEditPage), typeof(GenericEditPage));

            builder.Services.AddSingleton<ITemplateService, TemplateService>();
            builder.Services.AddTransient<SecCompositionViewModel>();
            builder.Services.AddTransient<SecCompositionPage>();

            // Сначала регистрируем парсер
            builder.Services.AddSingleton<ITemplateParserService, TemplateParserService>();
            // А затем сервис, который его использует
            builder.Services.AddSingleton<ITemplateService, TemplateService>();
            builder.Services.AddSingleton<IFolderPickerService, FolderPickerService>();
            builder.Services.AddTransient<IDocumentGenerationService, DocumentGenerationService>();
            builder.Services.AddSingleton<IPageSettingsService, PageSettingsService>();
            builder.Services.AddSingleton<IDefaultMappingService, DefaultMappingService>();

            return builder.Build();
        }
    }
}
