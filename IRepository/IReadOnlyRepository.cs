using Dapper;
using System.Linq.Expressions;

namespace DapperGenericRepository.IRepository
{
    public interface IReadOnlyRepository<T> : IDisposable
        where T : class
    {
        T Find(Expression<Func<T, bool>> predicate);
        IEnumerable<T> FindAll(Expression<Func<T, bool>> predicate);
        IEnumerable<T> FindAllIn(object list, Expression<Func<T, object>> InField, Expression<Func<T, bool>> predicate = null);
        IEnumerable<T> FindAllWithSelectedProperties(List<string> selectionFields, Expression<Func<T, bool>> predicate);
        IEnumerable<T> FindAllInWithSelectedProperties(object list, Expression<Func<T, object>> InField, List<string> selectionFields, Expression<Func<T, bool>> predicate);
        T FindTop1ByOrder(string CommaSeparatedColumnsForOrder, Expression<Func<T, bool>> predicate, bool isDesc = true);
        IEnumerable<T> GetAll();
        T GetById(object Id);
        T GetItem(string sqlQuery, DynamicParameters parameters = null);
        IEnumerable<T> GetItems(string sqlQuery, DynamicParameters parameters = null);
        bool IsValueUnique(string propertyName, object value, int idToExclude = 0);
    }
}
