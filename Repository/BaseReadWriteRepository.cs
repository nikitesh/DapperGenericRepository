using DapperGenericRepository.IRepository;
using System.Linq.Expressions;

namespace DapperGenericRepository.Repository
{
    internal class BaseReadWriteRepository<T> : BaseReadOnlyRepository<T>, IReadWriteRepository<T>
      where T : class
    {
        // To detect redundant calls
        private bool _disposed = false;
        private readonly IDapperRepositoryBase _dapperBaseRepository;
        public BaseReadWriteRepository(IDapperRepositoryBase dapperRepositoryBase) : base(dapperRepositoryBase)
        {
            _dapperBaseRepository = dapperRepositoryBase;
        }
        public bool Delete(T model)
        {
            return _dapperBaseRepository.Delete<T>(model);
        }
        public void DeleteByParam(Expression<Func<T, bool>> predicate)
        {
            _dapperBaseRepository.DeleteByParam<T>(predicate);
        }
        public int Insert(T model)
        {
            return _dapperBaseRepository.Insert<T>(model);
        }
        public void InsertList(List<T> modelList)
        {
            _dapperBaseRepository.InsertList<T>(modelList);
        }
        public void SoftDelete(T model)
        {
            _dapperBaseRepository.SoftDelete<T>(model);
        }
        public bool Update(T model)
        {
            return _dapperBaseRepository.Update<T>(model);
        }
        protected override void Dispose(bool disposing)
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
            base.Dispose(disposing);
        }

        public bool UpdateFewProperties(Dictionary<string, string> keyValues, Expression<Func<T, bool>> predicate)
        {
            return _dapperBaseRepository.UpdateFewProperties(keyValues, predicate);
        }
    }
}
