﻿using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Microsoft.Data.SqlClient;

namespace Custom_ORM.Data
{
    public class CustomDbContext
    {
        private readonly string _connectionString;
        public readonly Dictionary<string, object> _dbSets = new Dictionary<string, object>();

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
                string tablename = property.Name;
                CreateTableIfNotExists(entityType,tablename);
            }
        }

        //    protected void CreateTableIfNotExists(Type entityType, string tableName)
        //    {
        //        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //        var columns = properties.Select(p =>
        //        {
        //            var columnName = $"[{p.Name}]";
        //            var columnType = GetSqlColumnType(p,p.PropertyType);
        //            var isPrimaryKey = p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ? "PRIMARY KEY" : string.Empty;
        //            return $"{columnName} {columnType} {isPrimaryKey}".Trim();
        //        });

        //        if (!columns.Any())
        //        {
        //            throw new InvalidOperationException($"No columns defined for table {tableName}.");
        //        }

        //        var createTableSql = $@"
        //    IF NOT EXISTS (
        //        SELECT 1
        //        FROM INFORMATION_SCHEMA.TABLES
        //        WHERE TABLE_NAME = '{tableName}'
        //    )
        //    CREATE TABLE {tableName} ({string.Join(", ", columns)});
        //";
        //        Console.WriteLine(createTableSql);
        //        ExecuteSql(createTableSql);
        //    }

        //    protected void CreateTableIfNotExists(Type entityType, string tableName)
        //    {
        //        var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        //        var columns = new List<string>();
        //        var foreignKeyConstraints = new List<string>();

        //        foreach (var property in properties)
        //        {
        //            var columnName = $"[{property.Name}]";
        //            var columnType = GetSqlColumnType(property, property.PropertyType);
        //            var isPrimaryKey = property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ? "PRIMARY KEY" : string.Empty;

        //            // Handle ForeignKeyAttribute
        //            var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
        //            if (foreignKeyAttribute != null)
        //            {
        //                var referencedTable = foreignKeyAttribute.Name; // Should hold the table name
        //                var referencedColumn = "Id"; // Default primary key column
        //                foreignKeyConstraints.Add($"FOREIGN KEY ({property.Name}) REFERENCES {referencedTable}({referencedColumn})");
        //            }


        //            // Handle navigation properties
        //            if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
        //            {
        //                var navigationEntityType = property.PropertyType;
        //                var navigationTableName = navigationEntityType.Name; // Assuming pluralized table names
        //                var foreignKeyName = property.Name + "Id";
        //                //columns.Add($"[{foreignKeyName}] INT");
        //                foreignKeyConstraints.Add($"FOREIGN KEY ({foreignKeyName}) REFERENCES (Id)");
        //            }
        //            else (!string.IsNullOrEmpty(columnType))
        //            {
        //                columns.Add($"{columnName} {columnType} {isPrimaryKey}".Trim());
        //            }
        //        }

        //        if (!columns.Any())
        //        {
        //            throw new InvalidOperationException($"No columns defined for table {tableName}.");
        //        }

        //        // Create table SQL
        //        var createTableSql = $@"
        //    IF NOT EXISTS (
        //        SELECT 1
        //        FROM INFORMATION_SCHEMA.TABLES
        //        WHERE TABLE_NAME = '{tableName}'
        //    )
        //    CREATE TABLE {tableName} ({string.Join(", ", columns)});
        //";

        //        //Console.WriteLine(createTableSql);
        //        ExecuteSql(createTableSql);

        //        // Add foreign key constraints
        //        foreach (var fkConstraint in foreignKeyConstraints)
        //        {
        //            var alterTableSql = $@"ALTER TABLE {tableName} ADD {fkConstraint};";
        //            Console.WriteLine(alterTableSql);
        //            ExecuteSql(alterTableSql);
        //        }
        //    }





        protected void CreateTableIfNotExists(Type entityType, string tableName)
        {
            var properties = entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = new List<string>();
            var foreignKeyConstraints = new List<string>();

            foreach (var property in properties)
            {
                var columnName = $"[{property.Name}]";
                var columnType = GetSqlColumnType(property, property.PropertyType);
                var isPrimaryKey = property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ? "PRIMARY KEY" : string.Empty;

                // Handle ForeignKeyAttribute
                var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                if (foreignKeyAttribute != null)
                {
                    var referencedTable = foreignKeyAttribute.Name; // Should hold the table name
                    var referencedColumn = "Id"; // Default primary key column
                    columns.Add($"[{property.Name}] INT"); // Add the foreign key column
                    foreignKeyConstraints.Add($"FOREIGN KEY ({property.Name}) REFERENCES {referencedTable}s({referencedColumn})");
                    continue;
                }

                // Handle navigation properties
                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    var foreignKeyName = property.Name + "Id"; // Example: Property "Product" -> FK column "ProductId"
                    columns.Add($"[{foreignKeyName}] INT"); // Add foreign key column to the table
                    foreignKeyConstraints.Add($"FOREIGN KEY ({foreignKeyName}) REFERENCES {property.PropertyType.Name}s(Id)");
                }
                else if (!string.IsNullOrEmpty(columnType))
                {
                    columns.Add($"{columnName} {columnType} {isPrimaryKey}".Trim());
                }
            }

            if (!columns.Any())
            {
                throw new InvalidOperationException($"No columns defined for table {tableName}.");
            }

            // Create table SQL
            var createTableSql = $@"
    IF NOT EXISTS (
        SELECT 1
        FROM INFORMATION_SCHEMA.TABLES
        WHERE TABLE_NAME = '{tableName}'
    )
    CREATE TABLE {tableName} ({string.Join(", ", columns)});
";

            ExecuteSql(createTableSql);

            // Add foreign key constraints
            foreach (var fkConstraint in foreignKeyConstraints)
            {
                var alterTableSql = $@"ALTER TABLE {tableName} ADD {fkConstraint};";
                Console.WriteLine(alterTableSql);
                ExecuteSql(alterTableSql);
            }
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
            var isPrimaryKey = property.Name.Equals("Id", StringComparison.OrdinalIgnoreCase); // You can adjust this condition based on your actual primary key naming logic
                                                                                               // Handle navigation properties (skip complex types)
            if (underlyingType.IsClass && underlyingType != typeof(string))
            {
                return null; // Indicates it's not a column
            }
            return underlyingType switch
            {
                _ when underlyingType == typeof(int) => isPrimaryKey ? "INT IDENTITY(1,1)" : "INT",
                _ when underlyingType == typeof(string) => "NVARCHAR(MAX)",
                _ when underlyingType == typeof(DateTime) => "DATETIME",
                _ when underlyingType == typeof(bool) => "BIT",
                _ when underlyingType == typeof(decimal) => "DECIMAL(18, 2)",
                _ => throw new InvalidOperationException($"Unsupported type: {type.FullName}")
            };
        }

        public IEnumerable<T> Query<T>(string sql, params SqlParameter[] parameters) where T : class, new()
        {
            var entities = new List<T>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                using (var command = new SqlCommand(sql, connection))
                {
                    command.Parameters.AddRange(parameters);
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            var entity = new T();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                var property = entity.GetType().GetProperty(reader.GetName(i));
                                if (property != null && reader[i] != DBNull.Value)
                                {
                                    property.SetValue(entity, reader[i]);
                                }
                            }
                            entities.Add(entity);
                        }
                    }
                }
            }

            return entities;
        }
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
