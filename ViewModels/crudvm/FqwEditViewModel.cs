using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using EasySECv2.Models;
using EasySECv2.Services;

namespace EasySECv2.ViewModels
{
    /// <summary>
    /// View-model для редактирования итоговой квалификационной работы (FQW).
    /// Наследуется от общего GenericEditViewModel и использует всё его
    /// « CRUD-поведение » — валидацию, сохранение, отмену и т.п.
    /// </summary>
    public sealed class FqwEditViewModel
        : GenericEditViewModel<FinalQualifyingWork>, IGenericEditViewModel
    {
        private readonly DatabaseService _db;

        /* ------------------------------------------------------------------ */
        /*  Справочники, которые будут использоваться в динамических Picker-ах */
        /* ------------------------------------------------------------------ */

        /// <summary>Список сотрудников — потенц. руководителей ВКР</summary>
        public ObservableCollection<Staff> Supervisors { get; } = new();

        /* ------------------------------------------------------------------ */
        /*                          Передаваемые параметры                    */
        /* ------------------------------------------------------------------ */

        /// <summary>
        /// Идентификатор студента, передаётся через Query-property
        /// (&amp;studentId=…) и фиксируется в новой работе.
        /// </summary>
        public long StudentId { get; set; }

        /* ------------------------------------------------------------------ */
        /*                              ctor                                   */
        /* ------------------------------------------------------------------ */

        public FqwEditViewModel(ICrudService<FinalQualifyingWork> repo,
                                DatabaseService db)
            : base(repo)
        {
            _db = db;
            _ = LoadSupervisorsAsync();
        }

        /* ------------------------------------------------------------------ */
        /*             Подгружаем справочник руководителей из БД              */
        /* ------------------------------------------------------------------ */

        private async Task LoadSupervisorsAsync()
        {
            var list = await _db.GetAllStaffAsync();

            var eligible = list
                .Where(s => s.IsSupervisor)           // ← фильтр по IsSupervisor
                .OrderBy(s => s.Surname)              // сортируем для Picker-а
                .ToList();

            Supervisors.Clear();
            foreach (var s in eligible)
                Supervisors.Add(s);
        }

        /* ------------------------------------------------------------------ */
        /*         Загружаем существующую запись или создаём новую            */
        /* ------------------------------------------------------------------ */

        public new async Task LoadExistingAsync(long id)
        {
            // базовый метод либо загрузит сущность из БД (если id != 0),
            // либо создаст пустой Item и отметит IsNew = true
            await base.LoadExistingAsync(id);

            if (IsNew)
            {
                // фиксируем владельца и ставим сегодняшнюю дату
                Item.StudentId = StudentId;
                Item.Date = DateTime.Today;
            }
        }
    }
}
