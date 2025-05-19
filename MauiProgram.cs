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

            //
            // 1) Основной сервис базы
            //
            builder.Services.AddSingleton<DatabaseService>(sp =>
            {
                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var folder = Path.Combine(docs, "EasySEC");
                Directory.CreateDirectory(folder);
                var dbPath = Path.Combine(folder, "EasySEC.db3");
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

            //
            // 7) Роуты для навигации
            //
            Routing.RegisterRoute(nameof(EditStudentPage), typeof(EditStudentPage));
            Routing.RegisterRoute(nameof(EditDepartmentPage), typeof(EditDepartmentPage));
            Routing.RegisterRoute(nameof(EditOrientationPage), typeof(EditOrientationPage));
            Routing.RegisterRoute(nameof(EditInstitutePage), typeof(EditInstitutePage));
            Routing.RegisterRoute(nameof(EditFormOfEducationPage), typeof(EditFormOfEducationPage));

            return builder.Build();
        }
    }
}
