using Dapper;
using Dapper.Contrib.Extensions;
using DapperGenericRepository.CustomAttributes;
using DapperGenericRepository.IRepository;
using DapperGenericRepository.SqlHelpers;
using FastMember;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Text;

namespace DapperGenericRepository.Repository
{
    public class DapperRepositoryBase : IDapperRepositoryBase
    {
        private readonly string _connectionString;
        // To detect redundant calls
        private bool _disposed = false;
        public DapperRepositoryBase()
        {
            _connectionString = GetConnectionString();
        }

        /// <summary>
        /// Method for to insert bulk data.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataToSave"></param>
        /// <param name="storageParameters"></param>
        /// <param name="destinationTableName"></param>
        /// <param name="columnNamppings"></param>
        public void BulkInsertData<T>(List<T> dataToSave, string[] storageParameters, string destinationTableName, List<SqlBulkCopyColumnMapping> columnNamppings)
        {
            SqlConnection connection = new(_connectionString);
            SqlBulkCopy bulkCopy = new(connection.ConnectionString, SqlBulkCopyOptions.Default)
            {
                DestinationTableName = destinationTableName,
                //In my experience running bulk copy without a batch size specified will cause timeout issues. someone wisely said this!
                BatchSize = 10000,
                BulkCopyTimeout = 9999999
            };
            using ObjectReader reader = ObjectReader.Create(dataToSave, storageParameters);
            foreach (SqlBulkCopyColumnMapping mapping in columnNamppings)
            {
                _ = bulkCopy.ColumnMappings.Add(mapping);
            }
            bulkCopy.WriteToServer(reader);
        }

        /// <summary>
        /// Method to delete entity/data to database
        /// </summary>
        public virtual bool Delete<T>(T obj)
            where T : class
        {
            SqlConnection connection = new(_connectionString);
            return connection.Delete<T>(obj);
        }
        public void DeleteByParam<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateDeleteQuery<T>());
            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            SqlConnection connection = new(_connectionString);
            _ = connection.Execute(sqlBuilder.ToString().Trim(), dictionaryParams);

        }
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
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
                //_safeHandle?.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Method to execute the sql statement on database
        /// </summary>
        public virtual int Execute(CommandType commandType, string sql, object parameters = null)
        {
            SqlConnection connection = new(_connectionString);
            return connection.Execute(sql, parameters, commandType: commandType);
        }

        /// <summary>
        /// Method to Execute stored procedure on database
        /// </summary>
        public virtual void ExecuteStoreProcedure(string storedProcedure, object parameters = null)
        {
            _ = Execute(CommandType.StoredProcedure, storedProcedure, parameters);
        }

        public virtual T Find<T>(Expression<Func<T, bool>> predicate)
                   where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>());
            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            SqlConnection connection = new(_connectionString);
            return connection.QueryFirstOrDefault<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }

        public virtual IEnumerable<T> FindAll<T>(Expression<Func<T, bool>> predicate, string tableName = null)
                    where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = string.IsNullOrEmpty(tableName)
                ? sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>())
                : sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>(tableName));

            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }

        public IEnumerable<T> FindAllIn<T>(object list, Expression<Func<T, object>> InField, Expression<Func<T, bool>> predicate = null) where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>());
            _ = sqlBuilder.Append(" where ");
            _ = sqlBuilder.Append(ExpressionHelper.GetPropertyName(InField) + " IN @field");
            if (predicate != null)
            {
                _ = sqlBuilder.Append(" AND ");
                _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            }
            dictionaryParams.Add("@field", list);
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }

        public IEnumerable<T> FindAllInWithSelectedProperties<T>(object list, Expression<Func<T, object>> InField, List<string> selectionFields, Expression<Func<T, bool>> predicate) where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>(selectionFields));
            _ = sqlBuilder.Append(" where ");
            _ = sqlBuilder.Append(ExpressionHelper.GetPropertyName(InField) + " IN @field");
            if (predicate != null)
            {
                _ = sqlBuilder.Append(" AND ");
                _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            }
            dictionaryParams.Add("@field", list);
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }

        public IEnumerable<T> FindAllWithSelectedProperties<T>(List<string> selectionFields, Expression<Func<T, bool>> predicate) where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>(selectionFields));
            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }
        public virtual T FindTop1ByOrder<T>(string CommaSeparatedColumnsForOrder, Expression<Func<T, bool>> predicate, bool isDesc = true)
                  where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(DapperHelpers.GenerateSelectQuery<T>());
            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            _ = sqlBuilder.Append(" ORDER BY ");
            _ = sqlBuilder.Append(CommaSeparatedColumnsForOrder);
            if (isDesc)
            {
                _ = sqlBuilder.Append(" DESC");
            }
            SqlConnection connection = new(_connectionString);
            return connection.QueryFirstOrDefault<T>(sqlBuilder.ToString().Trim(), dictionaryParams);
        }
        /// <summary>
        /// Method to select all entities from database
        /// </summary>
        public virtual IEnumerable<T> GetAll<T>() where T : class
        {
            SqlConnection connection = new(_connectionString);
            return connection.GetAll<T>();
        }

        /// <summary>
        /// Method to select entity from database
        /// </summary>
        public virtual T GetByIdInt<T>(int id) where T : class
        {
            SqlConnection connection = new(_connectionString);
            return connection.Get<T>(id);
        }

        public virtual T GetByIdString<T>(string id) where T : class
        {
            SqlConnection connection = new(_connectionString);
            return connection.Get<T>(id);
        }

        /// <summary>
        /// Method to get an entity from database
        /// </summary>
        public virtual T GetItem<T>(CommandType commandType, string sql, object parameters = null)
        {
            SqlConnection connection = new(_connectionString);
            return connection.QueryFirstOrDefault<T>(sql, parameters, commandType: commandType);
        }

        /// <summary>
        /// Method to get the list of entities from database
        /// </summary>
        public virtual IEnumerable<T> GetItems<T>(CommandType commandType, string sql, object parameters = null)
        {
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sql, parameters, commandType: commandType);
        }

        public IEnumerable<T> GetSomeProperties<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Method to insert entity/data to database.
        /// Name of the table is by entity type name.
        /// </summary>
        public virtual int Insert<T>(T obj)
            where T : class
        {
            SqlConnection connection = new(_connectionString);
            return (int)connection.Insert(obj);
        }

        public void InsertList<T>(List<T> objList) where T : class
        {
            foreach (T item in objList)
            {
                _ = Insert<T>(item);
            }
        }

        /// <summary>
        /// Method to select entity/data from stored procedure asynchronously
        /// </summary>
        public virtual IEnumerable<T> SelectAllByStoredProcedure<T>(string sql, object parameters = null)
        {
            SqlConnection connection = new(_connectionString);
            return connection.Query<T>(sql, parameters, commandType: CommandType.StoredProcedure);
        }

        /// <summary>
        /// Method to select entity/data from stored procedure
        /// </summary>
        public virtual T SelectByStoredProcedure<T>(string sql, object parameters = null)
        {
            SqlConnection connection = new(_connectionString);
            return connection.QueryFirstOrDefault<T>(sql, parameters, commandType: CommandType.StoredProcedure);
        }
        public void SoftDelete<T>(T obj)
        {
            DapperHelpers.PropertyContainer propertyContainer = DapperHelpers.ParseProperties(obj, true);
            string sqlIdPairs = DapperHelpers.GetSqlPairs(propertyContainer.IdNames);

            string sqlQuery = string.Format("UPDATE [{0}] SET Is_Delete=1 WHERE {1}", typeof(T).Name, sqlIdPairs);
            _ = Execute(CommandType.Text, sqlQuery, propertyContainer.IdPairs);
        }
        /// <summary>
        /// Method to update entity/data to database.
        /// Name of the table is by entity type name.
        /// </summary>
        public virtual bool Update<T>(T obj)
            where T : class
        {
            SqlConnection connection = new(_connectionString);
            return connection.Update<T>(obj);
        }
        private static string GetConnectionString()
        {
            //return @"Data Source=WIN-V6PBOANUCV1\LUCIFER;Initial Catalog=Api_Database;Integrated Security=True";
            //return @"Data Source=DESKTOP-QV3NRI5;Initial Catalog=Api_Database;Integrated Security=True";
            return @"Data Source=" + Environment.GetEnvironmentVariable("DataBaseServer") + ";Initial Catalog=" + Environment.GetEnvironmentVariable("DataBaseName") + ";Integrated Security=True";
        }

        public bool UpdateFewProperties<T>(Dictionary<string, string> keyValues, Expression<Func<T, bool>> predicate) where T : class
        {
            IDictionary<string, object> dictionaryParams = new Dictionary<string, object>();
            StringBuilder sqlBuilder = new();
            _ = sqlBuilder.Append(string.Format("UPDATE {0} SET ", DapperHelpers.GetTableName<T>()));
            foreach (KeyValuePair<string, string> item in keyValues)
            {
                string paramKey = "@" + item.Key;
                dictionaryParams.Add(paramKey, item.Value);
                _ = sqlBuilder.Append(string.Format("{0} = {1},", item.Key, paramKey));
            }
            _ = sqlBuilder.Remove(sqlBuilder.Length - 1, 1);
            _ = sqlBuilder.Append(" where ");
            _ = SqlQueryHelper.Where<T>(sqlBuilder, predicate, ref dictionaryParams);
            SqlConnection connection = new(_connectionString);
            return connection.Execute(sqlBuilder.ToString().Trim(), dictionaryParams) > 0;
        }
    }
}
