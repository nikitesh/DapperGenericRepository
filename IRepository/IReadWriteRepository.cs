using System.Linq.Expressions;

namespace DapperGenericRepository.IRepository
{
    internal interface IReadWriteRepository<T> : IReadOnlyRepository<T>
        where T : class
    {
        bool Delete(T model);
        void DeleteByParam(Expression<Func<T, bool>> predicate);
        int Insert(T model);
        void InsertList(List<T> modelList);
        void SoftDelete(T model);
        bool Update(T model);
        bool UpdateFewProperties(Dictionary<string, string> keyValues, Expression<Func<T, bool>> predicate);
    }
}
