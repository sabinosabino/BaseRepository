
using System.Data;

namespace Sabino.BaseRepository
{
    public class RepositoryBase<T>
    {
        private readonly DbContext _db;
        public RepositoryBase(DbContext db)
        {
            _db = db;
        }
        public RepositoryBase(IDbConnection conn)
        {
            _db= new DbContext(conn);
        }
        public async Task<IEnumerable<T>> GetAll()
        {
            return await _db.QueryAsync<T>(new { });
        }
        public async Task<IEnumerable<T>> QueryIn(int[] ids, string campo = "id")
        {
            return await _db.QueryIn<T>(ids, campo);
        }
        public async Task<IEnumerable<T>> QueryIn(DateTime[] ids, string campo = "id")
        {
            return await _db.QueryIn<T>(ids, campo);
        }
        public async Task<IEnumerable<T>> QueryIn(string[] ids, string campo = "id")
        {
            return await _db.QueryIn<T>(ids, campo);
        }
        public async Task<int> DeleteIn(int[] ids, string campo = "id")
        {
            return await _db.DeleteIn<T>(ids, campo);
        }
        public async Task<int> DeleteIn(string[] ids, string campo = "id")
        {
            return await _db.DeleteIn<T>(ids, campo);
        }
        public async Task<int> NewId(string idColumn = "Id")
        {
            try
            {
                return await _db.NewIdAsync<T>(idColumn);
            }
            catch
            {
                return 1;
            }

        }
        public async Task<IEnumerable<T>> GetWhere(string expressao, object param)
        {
            return await _db.QueryAsync<T>(expressao, param);
        }
        public async Task<T> GetOne(int id)
        {
            return await _db.QueryFirstOrDefaultAsync<T>(new { Id = id });
        }
        public async Task<T> GetOne(string expressao, object param)
        {
            return await _db.QueryFirstOrDefaultAsync<T>(expressao, param);
        }
        public async Task<T> GetLast(string idColumn = "Id")
        {
            return await _db.GetLastInsertedAsync<T>(idColumn);
        }
        public async Task<int> Insert(T model, string ignore = "Id")
        {
            return await _db.InsertAsync<T>(model, ignore);
        }
        public async Task<int> Update(T model, string ignore = "Id")
        {
            return await _db.UpdateAsync<T>(model, ignore);
        }
        public async Task<int> Delete(int id)
        {
            return await _db.Delete<T>(new { Id = id });
        }
    }
}
