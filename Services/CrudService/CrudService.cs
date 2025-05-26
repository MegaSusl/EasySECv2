using SQLite;
using System;
using System.Threading.Tasks;

namespace EasySECv2.Services
{
    public class CrudService<T> : ICrudService<T>
        where T : class, new()
    {
        readonly SQLiteAsyncConnection _db;
        readonly Func<SQLiteAsyncConnection, AsyncTableQuery<T>> _queryFunc;
        readonly Func<SQLiteAsyncConnection, T, Task> _saveFunc;
        readonly Func<SQLiteAsyncConnection, T, Task> _deleteFunc;

        public AsyncTableQuery<T> Query { get; }

        public CrudService(
            SQLiteAsyncConnection db,
            Func<SQLiteAsyncConnection, AsyncTableQuery<T>> queryFunc,
            Func<SQLiteAsyncConnection, T, Task> saveFunc,
            Func<SQLiteAsyncConnection, T, Task> deleteFunc)
        {
            _db = db;
            _queryFunc = queryFunc;
            _saveFunc = saveFunc;
            _deleteFunc = deleteFunc;

            // сразу инициализируем Query
            Query = _queryFunc(_db);
        }
        public Task<T> FindByKeyAsync(object pk) => _db.FindAsync<T>(pk);
        public Task SaveAsync(T item) => _saveFunc(_db, item);

        public Task DeleteAsync(T item) => _deleteFunc(_db, item);
    }
}
