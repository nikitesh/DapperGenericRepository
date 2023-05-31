using Dapper;
using DapperGenericRepository.IRepository;
using System.Linq.Expressions;

namespace DapperGenericRepository.Repository
{
    public abstract class BaseReadOnlyRepository<T> : IReadOnlyRepository<T>
       where T : class
    {
        // To detect redundant calls
        private bool _disposed = false;
        private readonly IDapperRepositoryBase _dapperBaseRepository;
        public BaseReadOnlyRepository(IDapperRepositoryBase dapperRepositoryBase)
        {
            _dapperBaseRepository = dapperRepositoryBase;
        }
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }
        public T Find(Expression<Func<T, bool>> predicate)
        {
            return _dapperBaseRepository?.Find<T>(predicate);
        }

        public IEnumerable<T> FindAll(Expression<Func<T, bool>> predicate)
        {
            return _dapperBaseRepository.FindAll<T>(predicate);
        }

        public IEnumerable<T> FindAllIn(object list, Expression<Func<T, object>> InField, Expression<Func<T, bool>> predicate = default)
        {
            return _dapperBaseRepository.FindAllIn(list, InField, predicate);
        }

        public T FindTop1ByOrder(string CommaSeparatedColumnsForOrder, Expression<Func<T, bool>> predicate, bool isDesc = true)
        {
            return _dapperBaseRepository.FindTop1ByOrder(CommaSeparatedColumnsForOrder, predicate, isDesc);
        }

        public IEnumerable<T> GetAll()
        {
            return _dapperBaseRepository.GetAll<T>();
        }

        public T GetById(object Id)
        {
            return Id.GetType().ToString().ToLower().Contains("int")
                ? _dapperBaseRepository.GetByIdInt<T>((int)Id)
                : Id.GetType().ToString().ToLower() == "string" ? _dapperBaseRepository.GetByIdString<T>((string)Id) : null;
        }

        public T GetItem(string sqlQuery, DynamicParameters parameters = null)
        {
            return _dapperBaseRepository.GetItem<T>(System.Data.CommandType.Text, sqlQuery, parameters);
        }

        public IEnumerable<T> GetItems(string sqlQuery, DynamicParameters parameters)
        {
            return _dapperBaseRepository.GetItems<T>(System.Data.CommandType.Text, sqlQuery, parameters);
        }

        public bool IsValueUnique(string propertyName, object value, int idToExclude = 0)
        {
            throw new NotImplementedException();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // Dispose managed state (managed objects).                
                _dapperBaseRepository.Dispose();
            }

            _disposed = true;
        }

        public IEnumerable<T> FindAllWithSelectedProperties(List<string> selectionFields, Expression<Func<T, bool>> predicate)
        {
            return _dapperBaseRepository.FindAllWithSelectedProperties(selectionFields, predicate);
        }

        public IEnumerable<T> FindAllInWithSelectedProperties(object list, Expression<Func<T, object>> InField, List<string> selectionFields, Expression<Func<T, bool>> predicate)
        {
            return _dapperBaseRepository.FindAllInWithSelectedProperties(list, InField, selectionFields, predicate);
        }
    }
}
