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

namespace EasySECv2
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif

            // 1) Сервис базы данных (singleton)
            builder.Services.AddSingleton<DatabaseService>(sp =>
            {
                var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var folderPath = Path.Combine(documentsPath, "EasySEC");
                Directory.CreateDirectory(folderPath);
                var dbPath = Path.Combine(folderPath, "EasySEC.db3");
                return new DatabaseService(dbPath);
            });

            // 2) Помощник для импорта Excel
            builder.Services.AddSingleton<ExcelAdapter>();

            // 3) Универсальный CRUD-сервис
            builder.Services.AddSingleton<ICrudService<Student>>(sp =>
                new CrudService<Student>(
                    // передаём сам Async-connection
                    sp.GetRequiredService<DatabaseService>()._database,
                    // Func<SQLiteAsyncConnection, AsyncTableQuery<Student>>
                    db => db.Table<Student>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                )
            );
            builder.Services.AddSingleton<ICrudService<Department>>(sp =>
                new CrudService<Department>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Department>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                )
            );
            builder.Services.AddSingleton<ICrudService<Orientation>>(sp =>
                new CrudService<Orientation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Orientation>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                )
            );
            builder.Services.AddSingleton<ICrudService<Institute>>(sp =>
                new CrudService<Institute>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<Institute>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                )
            );
            builder.Services.AddSingleton<ICrudService<FormOfEducation>>(sp =>
                new CrudService<FormOfEducation>(
                    sp.GetRequiredService<DatabaseService>()._database,
                    db => db.Table<FormOfEducation>(),
                    (db, it) => it.id == 0 ? db.InsertAsync(it) : db.UpdateAsync(it),
                    (db, it) => db.DeleteAsync(it)
                )
            );
            // 4) StudentsPage и ViewModel
            builder.Services.AddTransient<StudentsViewModel>();
            builder.Services.AddTransient<StudentsPage>();

            // 5) Остальные страницы и VM
            builder.Services.AddTransient<DepartmentViewModel>();
            builder.Services.AddTransient<DepartmentPage>();

            builder.Services.AddTransient<OrientationViewModel>();
            builder.Services.AddTransient<OrientationPage>();

            builder.Services.AddTransient<InstituteViewModel>();
            builder.Services.AddTransient<InstitutePage>();

            builder.Services.AddTransient<FormOfEducationViewModel>();
            builder.Services.AddTransient<FormOfEducationPage>();

            builder.Services.AddTransient(typeof(GenericEditViewModel<>));

            // 4б) Страница редактирования
            builder.Services.AddTransient<GenericEditPage>();
            Routing.RegisterRoute(nameof(GenericEditPage), typeof(GenericEditPage));


            return builder.Build();
        }
    }
}
