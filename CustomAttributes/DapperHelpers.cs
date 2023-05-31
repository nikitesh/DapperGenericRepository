using Dapper.Contrib.Extensions;
using System.Data.SqlClient;
using System.Reflection;

namespace DapperGenericRepository.CustomAttributes
{
    public static class DapperHelpers
    {
        /// <summary>
        /// Method to check if the property is to be excluded from sql parameters
        /// </summary>
        public static bool ExcludeField(PropertyInfo prop)
        {
            System.Attribute attrs = prop.GetCustomAttribute(typeof(ComputedAttribute));
            return attrs != null;
        }

        public static List<string> GetNamesOfProperties<T>()
        {

            List<string> nameOfProperties = new();
            PropertyInfo[] properties = typeof(T).GetProperties();
            foreach (PropertyInfo property in properties)
            {
                if (ExcludeField(property))
                {
                    continue;
                }

                // Skip reference types (but still include string!)
                if (property.PropertyType.GetTypeInfo().IsClass && property.PropertyType != typeof(string) &&
                    property.PropertyType.GetTypeInfo().GenericTypeArguments.Length > 0)
                {
                    continue;
                }

                // Skip methods without a public setter
                if (property.GetSetMethod() == null)
                {
                    continue;
                }

                if (property.IsDefined(typeof(ComputedAttribute), false))
                {
                    continue;
                }

                // Skip methods specifically ignored
                if (property.IsDefined(typeof(WriteAttribute), false))
                {
                    continue;
                }

                nameOfProperties.Add(property.Name);
            }

            return nameOfProperties;
        }

        /// <summary>
        /// Create a commaseparated list of value pairs on 
        /// the form: "key1=@value1, key2=@value2, ..."
        /// </summary>
        public static string GetSqlPairs(IEnumerable<string> keys, string separator = ", ")
        {
            List<string> pairs = keys.Select(key => string.Format("[{0}]=@{0}", key)).ToList();
            return string.Join(separator, pairs);
        }

        public static string GetTableName<T>()
        {
            TableAttribute tAttribute = (TableAttribute)typeof(T).GetCustomAttribute(typeof(TableAttribute), true);
            return tAttribute.Name;
        }

        /// <summary>
        /// Method to get the sql parameters from entity properties
        /// </summary>
        public static PropertyContainer ParseProperties<T>(T obj, bool isWhereCondition = false)
        {
            PropertyContainer propertyContainer = new();

            string typeName = typeof(T).Name;
            //var validKeyNames = new[] { "Id", string.Format("{0}Id", typeName), string.Format("{0}_Id", typeName) };

            PropertyInfo[] properties = typeof(T).GetProperties();

            foreach (PropertyInfo property in properties)
            {
                //if (IsIdentityColumn(property))
                //    continue;

                if (ExcludeField(property))
                {
                    continue;
                }

                // Skip reference types (but still include string!)
                if (property.PropertyType.GetTypeInfo().IsClass && property.PropertyType != typeof(string) && property.PropertyType.GetTypeInfo().GenericTypeArguments.Length > 0)
                {
                    continue;
                }

                // Skip methods without a public setter
                if (property.GetSetMethod() == null)
                {
                    continue;
                }

                // Skip methods specifically ignored
                if (property.IsDefined(typeof(DapperIgnore), false))
                {
                    continue;
                }

                string name = property.Name;
                object value = typeof(T).GetProperty(property.Name).GetValue(obj, null);

                //if (value == null)
                //    value = value.GetType();

                if (isWhereCondition)
                {
                    bool excludeFieldFromUpdate = property.IsDefined(typeof(DapperExcludeInUpdate), false);
                    if (excludeFieldFromUpdate)
                    {
                        continue;
                    }

                    bool includeField = false;
                    bool isWhereField = property.IsDefined(typeof(DapperIncludeInWhere), false);

                    if (isWhereField)
                    {
                        if (property.GetCustomAttribute(typeof(DapperClassName)) is DapperClassName attr)
                        {
                            string className = attr.ClassName;
                            if (className.Contains(typeName))
                            {
                                includeField = true;
                            }
                        }
                    }

                    if (isWhereField && includeField)
                    {
                        propertyContainer.AddId(name, value);
                    }
                    else if (property.IsDefined(typeof(KeyAttribute), false))
                    {
                        propertyContainer.AddId(name, value);
                    }
                    else if (!property.IsDefined(typeof(DapperKey), false))
                    {
                        propertyContainer.AddValue(name, value);
                    }
                }
                else
                {
                    bool includeField = true;
                    if (property.GetCustomAttribute(typeof(DapperClassName)) is DapperClassName attr)
                    {
                        string className = attr.ClassName;
                        if (!className.Contains(typeName))
                        {
                            includeField = false;
                        }
                    }

                    if (includeField)
                    {
                        if (property.IsDefined(typeof(DapperKey), false))
                        {
                            propertyContainer.AddId(name, value);
                        }
                        else
                        {
                            propertyContainer.AddValue(name, value);
                        }
                    }
                }
            }

            return propertyContainer;
        }
        public static void SetId<T>(T obj, int id, IDictionary<string, object> propertyPairs)
        {
            if (propertyPairs.Count == 1)
            {
                string propertyName = propertyPairs.Keys.First();
                PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
                if (propertyInfo.PropertyType == typeof(int))
                {
                    propertyInfo.SetValue(obj, id, null);
                }
            }
        }
        public class PropertyContainer
        {
            public readonly Dictionary<string, object> _ids;
            public readonly Dictionary<string, object> _values;

            #region Properties

            /// <summary>
            /// All property names in property container
            /// </summary>
            public IEnumerable<string> AllNames => _ids.Keys.Union(_values.Keys);

            /// <summary>
            /// Property and value pair list
            /// </summary>
            public IEnumerable<KeyValuePair<string, object>> AllPairs => _values.Concat(_ids);

            public IEnumerable<string> AllParameters
            {
                get
                {
                    List<string> allParameters = new();

                    foreach (KeyValuePair<string, object> item in AllPairs)
                    {
                        allParameters.Add(string.Format("{0}={1}", item.Key, item.Value));
                    }
                    return allParameters;
                }
            }

            public IEnumerable<SqlParameter> AllSQLParameters
            {
                get
                {
                    List<SqlParameter> allParameters = new();

                    foreach (KeyValuePair<string, object> item in AllPairs)
                    {

                        allParameters.Add(new SqlParameter { ParameterName = string.Format("@{0}", item.Key), Value = item.Value });
                    }
                    return allParameters;
                }
            }

            /// <summary>
            /// Property Name in propertyContainer
            /// </summary>
            public IEnumerable<string> IdNames => _ids.Keys;

            /// <summary>
            /// All property and parameter pair
            /// </summary>
            public IDictionary<string, object> IdPairs => _ids;

            /// <summary>
            /// Property value in propertyContainer
            /// </summary>
            public IEnumerable<string> ValueNames => _values.Keys;
            /// <summary>
            /// All property and value parameters
            /// </summary>
            public IDictionary<string, object> ValuePairs => _values;
            #endregion

            #region Constructor

            public PropertyContainer()
            {
                _ids = new Dictionary<string, object>();
                _values = new Dictionary<string, object>();
            }

            #endregion

            #region Methods

            /// <summary>
            /// Method to add property in container
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            public void AddId(string name, object value)
            {
                _ids.Add(name, value);
            }

            /// <summary>
            /// Method to add the value of property in container
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            public void AddValue(string name, object value)
            {
                _values.Add(name, value);
            }

            #endregion
        }
        public static string GenerateSelectQuery<T>() where T : class
        {
            List<string> properties = GetNamesOfProperties<T>();
            return string.Format("select {0} FROM {1}", string.Join(',', properties), DapperHelpers.GetTableName<T>());
        }
        public static string GenerateSelectQuery<T>(List<string> selectedProperties) where T : class
        {
            return string.Format("select {0} FROM {1}", string.Join(',', selectedProperties), DapperHelpers.GetTableName<T>());
        }
        public static string GenerateSelectQuery<T>(string tableName) where T : class
        {
            List<string> properties = GetNamesOfProperties<T>();
            return string.Format("select {0} FROM {1}", string.Join(',', properties), tableName);
        }
        public static string GenerateDeleteQuery<T>() where T : class
        {
            return string.Format("Delete FROM {0}", DapperHelpers.GetTableName<T>());
        }
    }
}
