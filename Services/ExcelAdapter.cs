using EasySECv2.Models;
using ExcelDataReader;
using System.Text;
using Group = EasySECv2.Models.Group;

namespace EasySECv2.Services
{
    public class ExcelAdapter
    {
        public ExcelAdapter() { }

        public async Task<List<Student>> ReadStudentsFromExcel(string filePath, DatabaseService _databaseService)
        {
            var students = new List<Student>();
            long groupId = 0;

            try
            {
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
                var config = new ExcelReaderConfiguration
                {
                    FallbackEncoding = Encoding.GetEncoding("windows-1251")
                };
                using var reader = ExcelReaderFactory.CreateReader(stream, config);

                // если есть заголовок
                reader.Read();
                reader.Read();

                while (reader.Read())
                {
                    var fullName = reader.GetString(1) ?? "";
                    var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    var email = reader.GetString(2) ?? "";
                    var phone = reader.GetValue(3)?.ToString() ?? "";

                    // определяем/создаем группу из первой строки
                    if (groupId == 0)
                    {
                        var groupName = reader.GetString(4) ?? "";
                        var all = await _databaseService.GetAllGroupsAsync();
                        var exist = all.FirstOrDefault(g => g.name == groupName);
                        if (exist != null)
                            groupId = exist.id;
                        else
                            groupId = await _databaseService.SaveGroupAsync(new Group { name = groupName });
                    }

                    // создаём студента
                    var student = new Student
                    {
                        surname = parts.Length > 0 ? parts[0] : "",
                        name = parts.Length > 1 ? parts[1] : "",
                        middleName = parts.Length > 2 ? parts[2] : "",
                        email = email,
                        phone = phone,
                        groupId = groupId
                    };

                    students.Add(student);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Excel import error: {ex.Message}");
            }

            return students;
        }
    }
}
