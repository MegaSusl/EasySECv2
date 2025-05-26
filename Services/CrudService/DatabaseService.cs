using EasySECv2.Models;
using SQLite;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Reflection;

namespace EasySECv2.Services
{
    public class DatabaseService
    {
        public readonly SQLiteAsyncConnection _database;

        public DatabaseService(string dbPath)
        {
            _database = new SQLiteAsyncConnection(dbPath);
            InitializeDatabase();
        }

        // Вызываем асинхронную инициализацию без захвата контекста UI
        private void InitializeDatabase()
        {
            var initTask = InitializeAsync();
            initTask.ConfigureAwait(false);
            initTask.GetAwaiter().GetResult();
            System.Diagnostics.Debug.WriteLine("Database initialized at: " + _database.DatabasePath);
        }

        // Здесь создаём все таблицы и заливаем seed-данные
        private async Task InitializeAsync()
        {
            // создаём таблицы
            await _database.CreateTableAsync<Group>().ConfigureAwait(false);
            await _database.CreateTableAsync<Orientation>().ConfigureAwait(false);
            await _database.CreateTableAsync<FormOfEducation>().ConfigureAwait(false);
            await _database.CreateTableAsync<Institute>().ConfigureAwait(false);
            await _database.CreateTableAsync<Department>().ConfigureAwait(false);
            await _database.CreateTableAsync<Position>().ConfigureAwait(false);
            await _database.CreateTableAsync<Student>().ConfigureAwait(false);
            await _database.CreateTableAsync<Staff>().ConfigureAwait(false);
            await _database.CreateTableAsync<FinalQualifyingWork>().ConfigureAwait(false);

            await SeedOrientationsIfNeededAsync().ConfigureAwait(false);
        }
        public async Task SeedOrientationsIfNeededAsync()
        {
            // Если уже есть хоть одна запись — ничего не делаем
            var count = await _database.Table<Orientation>().CountAsync().ConfigureAwait(false);
            if (count > 0)
                return;

            try
            {
                // 1) Прочитать JSON из MauiAsset
                using var stream = await FileSystem.OpenAppPackageFileAsync("seedOrientations.json");
                using var reader = new StreamReader(stream);
                var json = await reader.ReadToEndAsync().ConfigureAwait(false);

                // 2) Распарсить в словарь code→name
                var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(
                    json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (dict == null || dict.Count == 0)
                    return;

                // 3) Преобразовать в список Orientation
                var list = dict.Select(kvp => new Orientation
                {
                    code = kvp.Key,
                    name = kvp.Value
                }).ToList();

                // 4) Вставить все за один раз
                await _database.InsertAllAsync(list).ConfigureAwait(false);
                System.Diagnostics.Debug.WriteLine($"Seeded {list.Count} orientations from JSON.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error seeding orientations: {ex}");
            }
        }


        public Task<List<Student>> GetStudentsAsync()
        {
            return _database.Table<Student>().ToListAsync();
        }

        public Task<List<Group>> GetAllGroupsAsync()
        {
            return _database.Table<Group>().ToListAsync();
        }

        public Task<List<Orientation>> GetAllOrientationsAsync()
        {
            return _database.Table<Orientation>().ToListAsync();
        }

        public async Task<long> SaveGroupAsync(Group group)
        {
            if (group.id != 0)
            {
                await _database.UpdateAsync(group).ConfigureAwait(false);
                return group.id;
            }
            else
            {
                await _database.InsertAsync(group).ConfigureAwait(false);
                return group.id;
            }
        }

        public Task<int> SaveStudentAsync(Student user)
        {
            return user.id != 0
                ? _database.UpdateAsync(user)
                : _database.InsertAsync(user);
        }

        public Task<int> DeleteStudentAsync(Student user)
            => _database.DeleteAsync(user);

        public async Task DeleteAllStudentsAsync()
        {
            await _database.DeleteAllAsync<Student>().ConfigureAwait(false);
        }

        public async Task DeleteAllGroupsAsync()
        {
            await _database.DeleteAllAsync<Group>().ConfigureAwait(false);
        }

        public Task<int> DeleteAllFromTableAsync(string tableName)
            => _database.ExecuteAsync($"DELETE FROM \"{tableName}\";");

        // Получение имён таблиц
        private class TableInfo { public string name { get; set; } }
        public async Task<List<string>> GetAllTableNamesAsync()
        {
            var results = await _database.QueryAsync<TableInfo>(
                "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';")
                .ConfigureAwait(false);
            return results
              .Select(t =>
              {
                  if (string.IsNullOrWhiteSpace(t.name))
                      return t.name;
                  return char.ToUpperInvariant(t.name[0]) + t.name.Substring(1);
              })
              .ToList();
        }

        public async Task<ObservableCollection<Department>> GetAllDepartmentsAsync()
        {
            var list = await _database.Table<Department>().ToListAsync().ConfigureAwait(false);
            return new ObservableCollection<Department>(list);
        }

        // Departments
        public Task SaveDepartmentAsync(Department d) => d.id == 0 ? _database.InsertAsync(d) : _database.UpdateAsync(d);
        public Task DeleteDepartmentAsync(Department d) => _database.DeleteAsync(d);

        // Orientations
        public Task SaveOrientationAsync(Orientation o) => o.id == 0 ? _database.InsertAsync(o) : _database.UpdateAsync(o);
        public Task DeleteOrientationAsync(Orientation o) => _database.DeleteAsync(o);

        // Institutes
        public Task<List<Institute>> GetAllInstitutesAsync() => _database.Table<Institute>().ToListAsync();
        public Task SaveInstituteAsync(Institute i) => i.id == 0 ? _database.InsertAsync(i) : _database.UpdateAsync(i);
        public Task DeleteInstituteAsync(Institute i) => _database.DeleteAsync(i);

        // Forms of Education
        public Task<List<FormOfEducation>> GetAllFormsOfEducationAsync() => _database.Table<FormOfEducation>().ToListAsync();
        public Task SaveFormOfEducationAsync(FormOfEducation f) => f.id == 0 ? _database.InsertAsync(f) : _database.UpdateAsync(f);
        public Task DeleteFormOfEducationAsync(FormOfEducation f) => _database.DeleteAsync(f);

        public Task<List<Staff>> GetAllStaffAsync() => _database.Table<Staff>().ToListAsync();
        public Task SaveStaffAsync(Staff f) => f.id == 0 ? _database.InsertAsync(f) : _database.UpdateAsync(f);
        public Task DeleteStaffAsync(Staff f) => _database.DeleteAsync(f);
        public ICrudService<Student> Students => new CrudService<Student>(
            _database,
            conn => conn.Table<Student>(),
            (db, item) => item.id == 0
                ? db.InsertAsync(item)
                : db.UpdateAsync(item),
            (db, item) => db.DeleteAsync(item)
        );
        public ICrudService<FormOfEducation> FormOfEducation => new CrudService<FormOfEducation>(
            _database,
            conn => conn.Table<FormOfEducation>(),
            (db, item) => item.id == 0
                ? db.InsertAsync(item)
                : db.UpdateAsync(item),
            (db, item) => db.DeleteAsync(item)
        );

        public List<string> GetAllTableNames()
            => GetAllTableNamesAsync().GetAwaiter().GetResult();

        //public List<object> GetAllByTableName(string tableName)
        //    => GetAllByTableNameAsync(tableName).GetAwaiter().GetResult();

        public async Task<List<object>> GetAllByTableNameAsync(string tableName)
        {
            var key = tableName?.Trim().ToLowerInvariant() ?? "";
            switch (key)
            {
                case "student":
                    return (await GetStudentsAsync()).Cast<object>().ToList();

                case "group":
                    return (await GetAllGroupsAsync()).Cast<object>().ToList();

                case "orientation":
                    return (await GetAllOrientationsAsync()).Cast<object>().ToList();

                case "formofeducation":
                    return (await GetAllFormsOfEducationAsync()).Cast<object>().ToList();

                case "institute":
                    return (await GetAllInstitutesAsync()).Cast<object>().ToList();

                case "department":
                    return (await GetAllDepartmentsAsync()).Cast<object>().ToList();

                case "staff":
                    return (await GetAllStaffAsync()).Cast<object>().ToList();

                //case nameof(Position):
                //    return (await GetAllStaffAsync()).Cast<object>().ToList();

                //case nameof(FinalQualifyingWork):
                //    return (await GetAllStaffAsync()).Cast<object>().ToList();

                // Добавьте здесь остальные сущности по аналогии…

                default:
                    return new List<object>();
            }
        }
    }
}
