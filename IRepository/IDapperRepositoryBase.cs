using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;

namespace DapperGenericRepository.IRepository
{
    public interface IDapperRepositoryBase : IDisposable
    {
        void BulkInsertData<T>(List<T> dataToSave, string[] storageParameters, string destinationTableName, List<SqlBulkCopyColumnMapping> columnNamppings);

        bool Delete<T>(T obj) where T : class;

        void DeleteByParam<T>(Expression<Func<T, bool>> predicate) where T : class;

        int Execute(CommandType commandType, string sql, object parameters = null);

        void ExecuteStoreProcedure(string storedProcedure, object parameters = null);

        T Find<T>(Expression<Func<T, bool>> predicate) where T : class;

        IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> predicate, string tableName = null) where T : class;
        IEnumerable<T> FindAllIn<T>(object list, Expression<Func<T, object>> InField, Expression<Func<T, bool>> predicate = null) where T : class;
        IEnumerable<T> FindAllWithSelectedProperties<T>(List<string> selectionFields, Expression<Func<T, bool>> predicate) where T : class;
        IEnumerable<T> FindAllInWithSelectedProperties<T>(object list, Expression<Func<T, object>> InField, List<string> selectionFields, Expression<Func<T, bool>> predicate) where T : class;
        T FindTop1ByOrder<T>(string CommaSeparatedColumnsForOrder, Expression<Func<T, bool>> predicate, bool isDesc = true) where T : class;

        IEnumerable<T> GetAll<T>() where T : class;

        T GetByIdInt<T>(int id) where T : class;

        T GetByIdString<T>(string id) where T : class;

        T GetItem<T>(CommandType commandType, string sql, object parameters = null);

        IEnumerable<T> GetItems<T>(CommandType commandType, string sql, object parameters = null);

        int Insert<T>(T obj) where T : class;
        void InsertList<T>(List<T> objList) where T : class;

        IEnumerable<T> SelectAllByStoredProcedure<T>(string sql, object parameters = null);

        T SelectByStoredProcedure<T>(string sql, object parameters = null);

        void SoftDelete<T>(T obj);

        bool Update<T>(T obj) where T : class;
        bool UpdateFewProperties<T>(Dictionary<string, string> keyValues, Expression<Func<T, bool>> predicate) where T : class;
        IEnumerable<T> GetSomeProperties<T>(Expression<Func<T, bool>> predicate) where T : class;
    }
}
