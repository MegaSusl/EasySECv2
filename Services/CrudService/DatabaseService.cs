using EasySECv2.Models;
using SQLite;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics;
using static EasySECv2.Services.DocumentGenerationService;

namespace EasySECv2.Services
{
    public class DatabaseService
    {
        public readonly SQLiteAsyncConnection _database;

        public DatabaseService(string dbPath)
        {
            var flags = SQLiteOpenFlags.ReadWrite |
            SQLiteOpenFlags.Create |
            SQLiteOpenFlags.SharedCache;

            _database = new SQLiteAsyncConnection(dbPath, flags);
            InitializeDatabase();
        }

        // Вызываем асинхронную инициализацию без захвата контекста UI
        private void InitializeDatabase()
        {
            var initTask = InitializeAsync();
            initTask.ConfigureAwait(false);
            initTask.GetAwaiter().GetResult();
            Trace.WriteLine("Database initialized at: " + _database.DatabasePath);
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
            await _database.CreateTableAsync<Student>().ConfigureAwait(false);
            await _database.CreateTableAsync<Staff>().ConfigureAwait(false);
            await _database.CreateTableAsync<FinalQualifyingWork>().ConfigureAwait(false);
            await _database.CreateTableAsync<Room>().ConfigureAwait(false);

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
                    Code = kvp.Key,
                    Name = kvp.Value
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
            if (group.Id != 0)
            {
                await _database.UpdateAsync(group).ConfigureAwait(false);
                return group.Id;
            }
            else
            {
                await _database.InsertAsync(group).ConfigureAwait(false);
                return group.Id;
            }
        }

        public Task<int> SaveStudentAsync(Student user)
        {
            return user.Id != 0
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
        public Task SaveDepartmentAsync(Department d) => d.Id == 0 ? _database.InsertAsync(d) : _database.UpdateAsync(d);
        public Task DeleteDepartmentAsync(Department d) => _database.DeleteAsync(d);

        // Orientations
        public Task SaveOrientationAsync(Orientation o) => o.Id == 0 ? _database.InsertAsync(o) : _database.UpdateAsync(o);
        public Task DeleteOrientationAsync(Orientation o) => _database.DeleteAsync(o);

        // Institutes
        public Task<List<Institute>> GetAllInstitutesAsync() => _database.Table<Institute>().ToListAsync();
        public Task SaveInstituteAsync(Institute i) => i.Id == 0 ? _database.InsertAsync(i) : _database.UpdateAsync(i);
        public Task DeleteInstituteAsync(Institute i) => _database.DeleteAsync(i);

        // Forms of Education
        public Task<List<FormOfEducation>> GetAllFormsOfEducationAsync() => _database.Table<FormOfEducation>().ToListAsync();
        public Task SaveFormOfEducationAsync(FormOfEducation f) => f.Id == 0 ? _database.InsertAsync(f) : _database.UpdateAsync(f);
        public Task DeleteFormOfEducationAsync(FormOfEducation f) => _database.DeleteAsync(f);

        public Task<List<Staff>> GetAllStaffAsync() => _database.Table<Staff>().ToListAsync();
        public Task SaveStaffAsync(Staff f) => f.Id == 0 ? _database.InsertAsync(f) : _database.UpdateAsync(f);
        public Task DeleteStaffAsync(Staff f) => _database.DeleteAsync(f);
        public ICrudService<Student> Students => new CrudService<Student>(
            _database,
            conn => conn.Table<Student>(),
            (db, item) => item.Id == 0
                ? db.InsertAsync(item)
                : db.UpdateAsync(item),
            (db, item) => db.DeleteAsync(item)
        );
        public ICrudService<FormOfEducation> FormOfEducation => new CrudService<FormOfEducation>(
            _database,
            conn => conn.Table<FormOfEducation>(),
            (db, item) => item.Id == 0
                ? db.InsertAsync(item)
                : db.UpdateAsync(item),
            (db, item) => db.DeleteAsync(item)
        );

        public List<string> GetAllTableNames()
            => GetAllTableNamesAsync().GetAwaiter().GetResult();

        //public List<object> GetAllByTableName(string tableName)
        //    => GetAllByTableNameAsync(tableName).GetAwaiter().GetResult();
        public Task<Orientation?> GetOrientationByIdAsync(long id)
            => _database.Table<Orientation>()
                        .Where(o => o.Id == id)
                        .FirstOrDefaultAsync();
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

        /* -----------------------------------------------------------------
         *  📄  Дополнительные методы для FamiliarizationPage и будущих CRUD
         * ----------------------------------------------------------------*/

        // ---------- Группы и студенты ----------
        #region Groups & Students

        /// <summary>
        /// Все группы (удобный псевдоним для уже имеющегося GetAllGroupsAsync)
        /// </summary>
        public Task<List<Group>> GetGroupsAsync()
            => GetAllGroupsAsync();

        /// <summary>
        /// Студенты выбранной группы
        /// </summary>
        public Task<List<Student>> GetStudentsByGroupAsync(long groupId)
            => _database.Table<Student>()
                        .Where(s => s.groupId == groupId)
                        .ToListAsync();

        /// <summary>
        /// Студенты нескольких групп (для пакетной генерации документов)
        /// </summary>
        public Task<List<Student>> GetStudentsByGroupsAsync(IEnumerable<long> groupIds)
            => _database.Table<Student>()
                        .Where(s => groupIds.Contains(s.groupId))
                        .ToListAsync();

        #endregion

        // ---------- Выпускные квалификационные работы ----------
        #region FinalQualifyingWork

        public Task<List<FinalQualifyingWork>> GetAllFinalQualifyingWorksAsync()
            => _database.Table<FinalQualifyingWork>().ToListAsync();

        public Task SaveFinalQualifyingWorkAsync(FinalQualifyingWork w)
            => w.Id == 0
                ? _database.InsertAsync(w)
                : _database.UpdateAsync(w);

        public Task DeleteFinalQualifyingWorkAsync(FinalQualifyingWork w)
            => _database.DeleteAsync(w);

        #endregion

        // ---------- Аудитории ----------  
        #region Rooms

        public Task<List<Room>> GetAllRoomsAsync()
            => _database.Table<Room>().ToListAsync();

        public Task SaveRoomAsync(Room room)
            => room.Id == 0
                ? _database.InsertAsync(room)
                : _database.UpdateAsync(room);

        public Task DeleteRoomAsync(Room room)
            => _database.DeleteAsync(room);

        #endregion
        public async Task<Staff?> GetStaffByIdAsync(long id)
        {
            return await _database.Table<Staff>()
                .Where(s => s.Id == id)
                .FirstOrDefaultAsync();
        }
        /* -----------------------------------------------------------------
         *   🔍  Поиск групп по Id и по названию
         * ----------------------------------------------------------------*/

        /// <summary>
        /// Группа по первичному ключу
        /// </summary>
        public Task<Group?> GetGroupByIdAsync(long id) =>
            _database.Table<Group>()
                     .Where(g => g.Id == id)
                     .FirstOrDefaultAsync();

        /// <summary>
        /// Группа по «человеческому» имени (exact match, регистр учитывается
        /// как в БД; если нужно без учёта регистра – допиши .ToLower()).
        /// </summary>
        public Task<Group?> GetGroupByNameAsync(string name) =>
            _database.Table<Group>()
                     .Where(g => g.Name == name)
                     .FirstOrDefaultAsync();

        /* -----------------------------------------------------------------
         *  DTO для строки таблицы тем ВКР
         * ----------------------------------------------------------------*/
        public sealed class VkrTopicInfo
        {
            public long StudentId { get; init; }
            public string StudentFio { get; init; } = "";
            public string OrientationCode { get; init; } = "";
            public string OrientationName { get; init; } = "";
            public string Topic { get; init; } = "";
            public string SupervisorFio { get; init; } = "";
            public string SupervisorPosition { get; init; } = "";
        }

        /* -----------------------------------------------------------------
         *  Темы ВКР всех студентов выбранной группы
         * ----------------------------------------------------------------*/
        public Task<List<VkrTopicInfo>> GetVkrTopicsByGroupAsync(long groupId)
        {
            const string sql = @"
                SELECT
                    s.Id                                                AS StudentId,
                    (s.Surname || ' ' || s.Name || ' ' ||
                     IFNULL(s.MiddleName,''))                           AS StudentFio,

                    IFNULL(o.Code,'')                                   AS OrientationCode,
                    IFNULL(o.Name,'')                                   AS OrientationName,

                    IFNULL(f.Topic,'')                                  AS Topic,

                    (IFNULL(st.Surname,'') || ' ' || IFNULL(st.Name,'') ||
                     CASE WHEN st.MiddleName IS NOT NULL
                          THEN ' ' || st.MiddleName ELSE '' END)        AS SupervisorFio,

                    IFNULL(st.Position,'')                              AS SupervisorPosition
                FROM Student                s
                LEFT JOIN Orientation       o  ON o.Id  = s.Orientation
                LEFT JOIN FinalQualifyingWork f  ON f.StudentId  = s.Id
                LEFT JOIN Staff             st ON st.Id = f.SupervisorId
                WHERE s.GroupId = ?;
            ";

            return _database.QueryAsync<VkrTopicInfo>(sql, groupId);
        }

        public Task<FinalQualifyingWork?> GetFqwByStudentIdAsync(long studentId) =>
                _database.Table<FinalQualifyingWork>()
                         .Where(f => f.StudentId == studentId)
                         .FirstOrDefaultAsync();
        public Task<Institute?> GetInstituteByIdAsync(long id) =>
               _database.Table<Institute>()
                        .Where(i => i.Id == id)
                        .FirstOrDefaultAsync();

        public async Task<GekResult> BuildGekResultAsync()
        {
            /* ──────────────────────────────────────────────────────────────
             * 0.  Выясняем Id форм обучения (ищем по названию)
             * ──────────────────────────────────────────────────────────────*/
            var forms = await GetAllFormsOfEducationAsync().ConfigureAwait(false);

            long fullId = forms.FirstOrDefault(f => f.Name.Contains("очная") &&
                                                     !f.Name.Contains("заоч"))?.Id ?? 0; // очная
            long mixedId = forms.FirstOrDefault(f => f.Name.Contains("очно-заоч"))?.Id ?? 0; // очно-заочная
            long partId = forms.FirstOrDefault(f => f.Name.StartsWith("Заоч") ||
                                                     f.Name.Contains("заочная"))?.Id ?? 0; // заочная

            /* небольшая утилита для scalar-запросов */
            async Task<int> CountAsync(string sql, params object[] args)
                => await _database.ExecuteScalarAsync<int>(sql, args).ConfigureAwait(false);

            /* ──────────────────────────────────────────────────────────────
             * 1.  Блок «Государственный экзамен»
             * ──────────────────────────────────────────────────────────────*/
            // 1.1  «допущены»  → признак Student.IsAccessed = 1
            const string SQL_EXAM_BASE = @"SELECT COUNT(*) FROM Student WHERE IsAccessed = 1 {0}";
            int exAdmAll = await CountAsync(string.Format(SQL_EXAM_BASE, ""));
            int exAdmFull = fullId == 0 ? 0 : await CountAsync(string.Format(SQL_EXAM_BASE, "AND FormOfEducation = ?"), fullId);
            int exAdmMix = mixedId == 0 ? 0 : await CountAsync(string.Format(SQL_EXAM_BASE, "AND FormOfEducation = ?"), mixedId);
            int exAdmPart = partId == 0 ? 0 : await CountAsync(string.Format(SQL_EXAM_BASE, "AND FormOfEducation = ?"), partId);

            /* 1.1 оценки и 1.2 неявки
               ─ в текущей модели таблицы госэкзамена нет, поэтому нули */
            int exA = 0, exB = 0, exC = 0, exD = 0;
            int exAbsAll = 0, exAbsFull = 0, exAbsMix = 0, exAbsPart = 0;

            /* ──────────────────────────────────────────────────────────────
             * 2.  Блок «Выпускная квалификационная работа»
             * ──────────────────────────────────────────────────────────────*/
            // 2.1  «принято к защите»  == простое наличие записи в fqw
            int fqwAccAll = await CountAsync("SELECT COUNT(*) FROM fqw");
            const string SQL_FQW_ACC_FORM = @"
        SELECT COUNT(*) FROM fqw f
        JOIN   Student              s ON s.Id = f.StudentId
        WHERE  s.FormOfEducation = ?";
            int fqwAccFull = fullId == 0 ? 0 : await CountAsync(SQL_FQW_ACC_FORM, fullId);
            int fqwAccMix = mixedId == 0 ? 0 : await CountAsync(SQL_FQW_ACC_FORM, mixedId);
            int fqwAccPart = partId == 0 ? 0 : await CountAsync(SQL_FQW_ACC_FORM, partId);

            // 2.2  «защищено»  (IsAttended = 1)
            int fqwDefAll = await CountAsync("SELECT COUNT(*) FROM fqw WHERE IsAttended = 1");
            const string SQL_FQW_DEF_FORM = @"
        SELECT COUNT(*) FROM fqw f
        JOIN   Student              s ON s.Id = f.StudentId
        WHERE  f.IsAttended = 1 AND s.FormOfEducation = ?";
            int fqwDefFull = fullId == 0 ? 0 : await CountAsync(SQL_FQW_DEF_FORM, fullId);
            int fqwDefMix = mixedId == 0 ? 0 : await CountAsync(SQL_FQW_DEF_FORM, mixedId);
            int fqwDefPart = partId == 0 ? 0 : await CountAsync(SQL_FQW_DEF_FORM, partId);

            // 2.3  оценки (A–D) среди защищённых
            async Task<int> FqwMarkAsync(int mark) =>
                await CountAsync("SELECT COUNT(*) FROM fqw WHERE IsAttended = 1 AND Mark = ?", mark);
            int fqwA = await FqwMarkAsync(5);
            int fqwB = await FqwMarkAsync(4);
            int fqwC = await FqwMarkAsync(3);
            int fqwD = await FqwMarkAsync(2);

            // 2.4  «не явились»
            int fqwAbsAll = await CountAsync("SELECT COUNT(*) FROM fqw WHERE IsAttended = 0");
            const string SQL_FQW_ABS_FORM = @"
        SELECT COUNT(*) FROM fqw f
        JOIN   Student              s ON s.Id = f.StudentId
        WHERE  f.IsAttended = 0 AND s.FormOfEducation = ?";
            int fqwAbsFull = fullId == 0 ? 0 : await CountAsync(SQL_FQW_ABS_FORM, fullId);
            int fqwAbsMix = mixedId == 0 ? 0 : await CountAsync(SQL_FQW_ABS_FORM, mixedId);
            int fqwAbsPart = partId == 0 ? 0 : await CountAsync(SQL_FQW_ABS_FORM, partId);

            // 2.5  «переносы»  – нет данных
            int fqwPostAll = 0, fqwPostFull = 0, fqwPostMix = 0, fqwPostPart = 0;

            // 2.6-2.9  (типы ВКР, рекомендации, оригинальность) – пока нули
            int fqwResearch = 0, fqwPractice = 0, fqwProject = 0, fqwStartup = 0, fqwSocial = 0;
            int fqwToPublish = 0, fqwToImplement = 0, fqwImplemented = 0;
            int honourDiplomas = 0;
            double? avgOriginality = null;

            /* ──────────────────────────────────────────────────────────────
             * 3.  Собираем DTO и возвращаем
             * ──────────────────────────────────────────────────────────────*/
            return new GekResult(
                // 1.1
                exAdmAll, exAdmFull, exAdmMix, exAdmPart,
                // 1.1 оценки
                exA, exB, exC, exD,
                // 1.2
                exAbsAll, exAbsFull, exAbsMix, exAbsPart,

                // 2.1
                fqwAccAll, fqwAccFull, fqwAccMix, fqwAccPart,
                // 2.2
                fqwDefAll, fqwDefFull, fqwDefMix, fqwDefPart,
                // 2.3
                fqwA, fqwB, fqwC, fqwD,
                // 2.4
                fqwAbsAll, fqwAbsFull, fqwAbsMix, fqwAbsPart,
                // 2.5
                fqwPostAll, fqwPostFull, fqwPostMix, fqwPostPart,

                // 2.6-2.9
                fqwResearch, fqwPractice, fqwProject, fqwStartup, fqwSocial,
                fqwToPublish, fqwToImplement, fqwImplemented,
                honourDiplomas,
                avgOriginality
            );
        }
    }
}
