



using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Custom_ORM.Data
{
    public class CustomDbContext
    {
        public readonly string _connectionString;
        private readonly Dictionary<string, object> _dbSets = new Dictionary<string, object>();

        public CustomDbContext(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTablesCreated();
        }

        public DbSet<T> Set<T>() where T : class
        {
            var typeName = typeof(T).Name;
            if (!_dbSets.ContainsKey(typeName))
            {
                _dbSets[typeName] = new DbSet<T>(this);
            }

            return (DbSet<T>)_dbSets[typeName];
        }

        public void EnsureTablesCreated()
        {
            var dbSetProperties = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GetGenericArguments().First();
                CreateTableIfNotExists(entityType, property.Name);
            }
        }




        protected void CreateTableIfNotExists(Type entityType, string tableName)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = new List<string>();

            foreach (var property in properties)
            {
                var columnName = $"[{property.Name}]";
                var columnType = GetSqlColumnType(property, property.PropertyType);
                var isPrimaryKey = property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ? "PRIMARY KEY IDENTITY(1,1)" : string.Empty;

                columns.Add($"{columnName} {columnType} {isPrimaryKey}".Trim());
            }

            if (!columns.Any())
                throw new InvalidOperationException($"No columns defined for table {tableName}.");

            var createTableSql = $@"
            IF NOT EXISTS (
                SELECT 1
                FROM INFORMATION_SCHEMA.TABLES
                WHERE TABLE_NAME = '{tableName}'
            )
            CREATE TABLE {tableName} ({string.Join(", ", columns)});
        ";

            ExecuteSql(createTableSql);
        }

        public void ExecuteSql(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddRange(parameters);
                    command.ExecuteNonQuery();
                }
            }
        }

        private string GetSqlColumnType(PropertyInfo property, Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;

            return underlyingType switch
            {
                _ when underlyingType == typeof(int) => "INT",
                _ when underlyingType == typeof(string) => "NVARCHAR(MAX)",
                _ when underlyingType == typeof(DateTime) => "DATETIME",
                _ when underlyingType == typeof(bool) => "BIT",
                _ when underlyingType == typeof(decimal) => "DECIMAL(18, 2)",
                _ => throw new InvalidOperationException($"Unsupported type: {type.FullName}")
            };
        }

        //public IEnumerable<T> Query<T>(string sql, params SqlParameter[] parameters) where T : class, new()
        //{
        //    var entities = new List<T>();

        //    using (var connection = new SqlConnection(_connectionString))
        //    {
        //        connection.Open();
        //        using (var command = new SqlCommand(sql, connection))
        //        {
        //            command.Parameters.AddRange(parameters);
        //            using (var reader = command.ExecuteReader())
        //            {
        //                while (reader.Read())
        //                {
        //                    var entity = new T();
        //                    for (int i = 0; i < reader.FieldCount; i++)
        //                    {
        //                        var property = entity.GetType().GetProperty(reader.GetName(i));
        //                        if (property != null && reader[i] != DBNull.Value)
        //                        {
        //                            property.SetValue(entity, reader[i]);
        //                        }
        //                    }
        //                    entities.Add(entity);
        //                }
        //            }
        //        }
        //    }

        //    return entities;
        //}
    }

    public class DbSet<T> where T : class
    {
        private readonly CustomDbContext _context;

        public DbSet(CustomDbContext context)
        {
            _context = context;
        }

        public void Add(T entity)
        {
            var sql = GenerateInsertSql(entity);
            var parameters = GenerateSqlParameters(entity);
            _context.ExecuteSql(sql, parameters.ToArray());
        }

        public List<T> GetAll()
        {
            var tableName = typeof(T).Name + "s";
            var sql = $"SELECT * FROM {tableName}";
            var data = new List<T>();

            using (var connection = new SqlConnection(_context._connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        var properties = typeof(T).GetProperties();

                        while (reader.Read())
                        {
                            var entity = Activator.CreateInstance<T>();
                            foreach (var property in properties)
                            {
                                if (reader[property.Name] != DBNull.Value)
                                {
                                    property.SetValue(entity, reader[property.Name]);
                                }
                            }
                            data.Add(entity);
                        }
                    }
                }
            }

            return data;
        }



        public void Update(T entity)
        {
            var tableName = typeof(T).Name + "s";
            var columns = entity.GetType().GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => $"{p.Name} = @{p.Name}");

            var sql = $"UPDATE {tableName} SET {string.Join(", ", columns)} WHERE Id = @Id";
            var parameters = GenerateSqlParameters(entity);

            _context.ExecuteSql(sql, parameters.ToArray());
        }

        public void Delete(int id)
        {
            var tableName = typeof(T).Name + "s";
            var sql = $"DELETE FROM {tableName} WHERE Id = @Id";
            var parameter = new SqlParameter("@Id", id);

            _context.ExecuteSql(sql, parameter);
        }



        public T GetById(int id)
        {
            var tableName = typeof(T).Name + "s";
            var sql = $"SELECT * FROM {tableName} WHERE Id = @Id";
            var parameter = new SqlParameter("@Id", id);
            var result = new List<T>();

            using (var connection = new SqlConnection(_context._connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.Add(parameter);
                    using (var reader = command.ExecuteReader())
                    {
                        var properties = typeof(T).GetProperties();

                        while (reader.Read())
                        {
                            var entity = Activator.CreateInstance<T>();
                            foreach (var property in properties)
                            {
                                if (reader[property.Name] != DBNull.Value)
                                {
                                    property.SetValue(entity, reader[property.Name]);
                                }
                            }
                            result.Add(entity);
                        }
                    }
                }
            }

            return result.SingleOrDefault();
        }


        private string GenerateInsertSql(T entity)
        {
            var tableName = typeof(T).Name + "s";
            var columns = string.Join(", ", entity.GetType().GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => p.Name));

            var values = string.Join(", ", entity.GetType().GetProperties()
                .Where(p => !p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase))
                .Select(p => "@" + p.Name));

            return $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
        }

        private List<SqlParameter> GenerateSqlParameters(T entity)
        {
            var parameters = new List<SqlParameter>();
            foreach (var property in entity.GetType().GetProperties())
            {
                var value = property.GetValue(entity);
                var parameter = new SqlParameter("@" + property.Name, value ?? DBNull.Value);
                parameters.Add(parameter);
            }
            return parameters;
        }
    }
}

