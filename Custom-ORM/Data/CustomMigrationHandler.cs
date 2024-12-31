//using System;
//using System.ComponentModel.DataAnnotations.Schema;
//using System.IO;
//using System.Reflection;
//using Microsoft.AspNetCore.Http.Features;

//namespace Custom_ORM.Data
//{
//    public class CustomMigrationHandler
//    {
//        private readonly MyCustomDbContext _context;
//        private readonly string _snapshotPath = "schema_snapshot.json";
//        public CustomMigrationHandler(MyCustomDbContext context)
//        {
//            _context = context;
//        }
//        public void AddMigration(string migrationName)
//        {
//            //var migrationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
//            string migrationsDir = "C:\\Users\\Bhaskara Cheruku\\source\\repos\\Custom-ORM\\Custom-ORM\\Migrations\\";
//            if (!Directory.Exists(migrationsDir))
//                Directory.CreateDirectory(migrationsDir);

//            var migrationFile = Path.Combine(migrationsDir, $"{migrationName}_{DateTime.Now:yyyyMMddHHmmss}.sql");

//            // Generate SQL script (replace this with actual schema comparison logic)
//            Console.WriteLine("Started....");
//            var migrationScript = GenerateMigrationScript();

//            File.WriteAllText(migrationFile, migrationScript);
//            Console.WriteLine($"Migration '{migrationName}' created at: {migrationFile}");
//        }

//        public void UpdateDatabase()
//        {
//            var migrationsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Migrations");
//            if (!Directory.Exists(migrationsDir))
//            {
//                Console.WriteLine("No migrations directory found.");
//                return;
//            }

//            foreach (var file in Directory.GetFiles(migrationsDir, "*.sql"))
//            {
//                Console.WriteLine($"Applying migration: {Path.GetFileName(file)}");

//                var script = File.ReadAllText(file);
//                _context.ExecuteSql(script); // Custom method to execute raw SQL
//            }

//            Console.WriteLine("Database updated with all migrations.");
//        }
//        private string GenerateMigrationScript()
//        {
//            Console.WriteLine("1....");
//            var currentSnapshot = GetCurrentSchemaSnapshot();

//            if (File.Exists(_snapshotPath))
//            {
//                Console.WriteLine("OLD");
//                var previousSnapshot = LoadPreviousSchemaSnapshot();
//                return CompareSchemasAndGenerateSql(previousSnapshot, currentSnapshot);
//            }
//            else
//            {
//                Console.WriteLine("New");
//                // If no previous snapshot exists, assume this is the first migration and just create tables
//                return CreateInitialMigrationSql(currentSnapshot);
//            }
//        }

//        private string CompareSchemasAndGenerateSql(SchemaSnapshot previousSnapshot, SchemaSnapshot currentSnapshot)
//        {
//            var script = "-- SQL Migration Script\n" +
//                         "-- Add your schema changes here.\n";

//            // Compare tables in previous and current schema and generate ALTER/DROP/ADD statements
//            foreach (var currentTable in currentSnapshot.Tables)
//            {
//                var tableName = currentTable.Key;
//                var currentColumns = currentTable.Value.Columns;
//                var previousColumns = previousSnapshot.Tables.ContainsKey(tableName)
//                    ? previousSnapshot.Tables[tableName].Columns
//                    : new List<string>();

//                // Detect added or removed columns
//                var addedColumns = currentColumns.Except(previousColumns).ToList();
//                var removedColumns = previousColumns.Except(currentColumns).ToList();

//                if (addedColumns.Any())
//                    script += $"-- Add columns to table {tableName}: {string.Join(", ", addedColumns)}\n";
//                if (removedColumns.Any())
//                    script += $"-- Drop columns from table {tableName}: {string.Join(", ", removedColumns)}\n";
//            }

//            // Add more comparisons for other schema elements like foreign keys, indexes, etc.
//            return script;
//        }
//        private string CreateInitialMigrationSql(SchemaSnapshot currentSnapshot)
//        {
//            var script = "-- Initial Schema Creation Script\n";
//            foreach (var table in currentSnapshot.Tables)
//            {
//                var tableName = table.Key;
//                var columns = string.Join(", ", table.Value.Columns);
//                script += $"CREATE TABLE {tableName} ({columns});\n";
//            }

//            return script;
//        }

//        private void SaveSchemaSnapshot()
//        {
//            var currentSnapshot = GetCurrentSchemaSnapshot();
//            var json = SchemaSnapshot.Serialize(currentSnapshot);
//            File.WriteAllText(_snapshotPath, json);
//        }

//        //private SchemaSnapshot GetCurrentSchemaSnapshot()
//        //{
//        //    var snapshot = new SchemaSnapshot();

//        //    var model = _context._dbSets;
//        //    foreach (var entityType in model.GetEntityTypes())
//        //    {
//        //        var tableSchema = new SchemaSnapshot.TableSchema();

//        //        foreach (var property in entityType.GetProperties())
//        //        {
//        //            tableSchema.Columns.Add(property.Name);
//        //        }

//        //        foreach (var foreignKey in entityType.GetForeignKeys())
//        //        {
//        //            tableSchema.ForeignKeys.Add(foreignKey.PrincipalEntityType.Name);
//        //        }

//        //        snapshot.Tables[entityType.GetTableName()] = tableSchema;
//        //    }

//        //    return snapshot;
//        //}
//        public SchemaSnapshot GetCurrentSchemaSnapshot()
//        {
//            var snapshot = new SchemaSnapshot();

//            foreach (var dbSetEntry in _context._dbSets)
//            {
//                var entityType = dbSetEntry.Value.GetType().GenericTypeArguments.First(); // Get the entity type of DbSet<T>
//                var tableSchema = new SchemaSnapshot.TableSchema();

//                foreach (var property in entityType.GetProperties())
//                {
//                    tableSchema.Columns.Add(property.Name);
//                }
//                foreach (var property in entityType.GetProperties())
//                {
//                    var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
//                    if (foreignKeyAttribute != null)
//                    {
//                        tableSchema.ForeignKeys.Add(foreignKeyAttribute.Name);
//                    }
//                }

//                snapshot.Tables[entityType.Name + "s"] = tableSchema;
//            }

//            return snapshot;
//        }


//        private SchemaSnapshot LoadPreviousSchemaSnapshot()
//        {
//            var json = File.ReadAllText(_snapshotPath);
//            return SchemaSnapshot.Deserialize(json);
//        }

//    }
//}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json; // Use Newtonsoft.Json for JSON serialization/deserialization

namespace Custom_ORM.Data
{
    public class CustomMigrationHandler
    {
        private readonly MyCustomDbContext _context;
        private readonly string _snapshotPath = "schema_snapshot.json";
        private readonly string _migrationsDir = "Migrations";

        public CustomMigrationHandler(MyCustomDbContext context)
        {
            _context = context;
        }

        public void AddMigration(string migrationName)
        {
            EnsureMigrationDirectoryExists();

            var migrationFile = Path.Combine(_migrationsDir, $"{migrationName}_{DateTime.Now:yyyyMMddHHmmss}.sql");

            Console.WriteLine("Started migration generation...");
            var migrationScript = GenerateMigrationScript();

            File.WriteAllText(migrationFile, migrationScript);
            SaveSchemaSnapshot(); // Save current schema as the new snapshot

            Console.WriteLine($"Migration '{migrationName}' created at: {migrationFile}");
        }

        public void UpdateDatabase()
        {
            EnsureMigrationDirectoryExists();

            var migrationFiles = Directory.GetFiles(_migrationsDir, "*.sql");
            if (!migrationFiles.Any())
            {
                Console.WriteLine("No migrations found to apply.");
                return;
            }

            foreach (var file in migrationFiles)
            {
                Console.WriteLine($"Applying migration: {Path.GetFileName(file)}");
                var script = File.ReadAllText(file);
                _context.ExecuteSql(script); // Custom method to execute raw SQL
            }

            Console.WriteLine("Database updated with all migrations.");
        }

        private string GenerateMigrationScript()
        {
            Console.WriteLine("Generating migration script...");
            var currentSnapshot = GetCurrentSchemaSnapshot();

            if (File.Exists(_snapshotPath))
            {
                Console.WriteLine("Loading previous schema snapshot...");
                var previousSnapshot = LoadPreviousSchemaSnapshot();
                return CompareSchemasAndGenerateSql(previousSnapshot, currentSnapshot);
            }

            Console.WriteLine("No previous snapshot found. Generating initial schema script...");
            return CreateInitialMigrationSql(currentSnapshot);
        }

        private string MapClrTypeToSqlType(Type clrType)
        {
            if (clrType == typeof(int) || clrType == typeof(long) || clrType == typeof(short) || clrType == typeof(byte))
                return "INT";
            if (clrType == typeof(decimal) || clrType == typeof(float) || clrType == typeof(double))
                return "DECIMAL(18, 2)";
            if (clrType == typeof(string))
                return "VARCHAR(MAX)";
            if (clrType == typeof(bool))
                return "BIT";
            if (clrType == typeof(DateTime))
                return "DATETIME";
            if (clrType == typeof(Guid))
                return "UNIQUEIDENTIFIER";
            if (clrType == typeof(byte[]))
                return "VARBINARY(MAX)";
            if (clrType == typeof(DateTimeOffset))
                return "DATETIMEOFFSET";
            if (clrType == typeof(TimeSpan))
                return "TIME";

            // For nullable types
            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return MapClrTypeToSqlType(Nullable.GetUnderlyingType(clrType));

            // Default case for unknown types
            return "VARCHAR(255)";
        }

        private string CompareSchemasAndGenerateSql(SchemaSnapshot previousSnapshot, SchemaSnapshot currentSnapshot)
        {
            var script = "-- SQL Migration Script\n";
            Console.WriteLine("Entered");
            foreach (var currentTable in currentSnapshot.Tables)
            {
                Console.WriteLine(currentTable.Key );
                var tableName = currentTable.Key;
                var currentColumns = currentTable.Value.Columns;
                var previousColumns = previousSnapshot.Tables.ContainsKey(tableName)
                    ? previousSnapshot.Tables[tableName].Columns
                    : new List<string>();
                
                var addedColumns = currentColumns.Except(previousColumns).ToList();
                var removedColumns = previousColumns.Except(currentColumns).ToList();

                foreach (var column in addedColumns)
                {

                    // Example column string: "ColumnName DataType"
                    string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                    {
                        // Handle invalid column definitions
                        throw new ArgumentException($"Invalid column definition: {column}");
                    }

                    string columnName = parts[0]; // The first part is the column name
                    string dataType = parts[1];  // The second part is the data type

                    // If there's additional metadata (like NOT NULL), include it
                    string additionalAttributes = string.Join(" ", parts.Skip(2));
                    Console.WriteLine(columnName);
                    string[] validTypes = { "INT", "VARCHAR(MAX)", "DATETIME", "VARCHAR(255)" }; // Add all valid types
                    if (!validTypes.Contains(dataType.ToUpper()))
                    {
                        throw new ArgumentException($"Unsupported column type: {dataType}");
                    }
                    // Construct the ALTER TABLE statement
                    script += $"ALTER TABLE {tableName} ADD {columnName} {dataType} {additionalAttributes};\n";
                }

                foreach (var column in removedColumns)
                {

                    // Example column string: "ColumnName DataType"
                    string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                    if (parts.Length < 2)
                    {
                        // Handle invalid column definitions
                        throw new ArgumentException($"Invalid column definition: {column}");
                    }

                    string columnName = parts[0]; // The first part is the column name
                    string dataType = parts[1];  // The second part is the data type


                    script += $"ALTER TABLE {tableName} DROP COLUMN {columnName};\n";
                }
            }

            // Handle foreign keys or other schema changes if needed

            return script;
        }

        private string CreateInitialMigrationSql(SchemaSnapshot currentSnapshot)
        {
            var script = "-- Initial Schema Creation Script\n";
            Console.WriteLine("Entered");
            foreach (var table in currentSnapshot.Tables)
            {
                Console.WriteLine(table.Key + " " + table.Value);
                var tableName = table.Key;
                var columns = string.Join(", ", table.Value.Columns.Select(c => $"{c} VARCHAR(255)")); // Example type
                script += $"CREATE TABLE {tableName} ({columns});\n";
            }

            return script;
        }

        private void SaveSchemaSnapshot()
        {
            var currentSnapshot = GetCurrentSchemaSnapshot();
            var json = JsonConvert.SerializeObject(currentSnapshot, Formatting.Indented);
            File.WriteAllText(_snapshotPath, json);
        }

        private SchemaSnapshot GetCurrentSchemaSnapshot()
        {
            var snapshot = new SchemaSnapshot();

            
            // Retrieve DbSet properties using reflection
            var dbSetProperties = _context.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                .ToList();

            if (!dbSetProperties.Any())
            {
                Console.WriteLine("No DbSet properties found in the context.");
                return snapshot;
            }

            Console.WriteLine("DbSet properties found:");
            foreach (var dbSetProperty in dbSetProperties)
            {
                Console.WriteLine($" - {dbSetProperty.Name}");

                // Get the entity type of DbSet<T>
                var entityType = dbSetProperty.PropertyType.GetGenericArguments().FirstOrDefault();
                if (entityType == null)
                {
                    Console.WriteLine($"Failed to retrieve entity type for {dbSetProperty.Name}.");
                    continue;
                }

                // Create a new TableSchema for this entity
                var tableSchema = new SchemaSnapshot.TableSchema();

                foreach (var property in entityType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    string columnName = property.Name;
                    string columnType = MapClrTypeToSqlType(property.PropertyType);

                    // Add the column with its SQL type
                    tableSchema.Columns.Add($"{columnName} {columnType}");

                    // Check for ForeignKey attributes
                    var foreignKeyAttribute = property.GetCustomAttribute<ForeignKeyAttribute>();
                    if (foreignKeyAttribute != null)
                    {
                        tableSchema.ForeignKeys.Add(foreignKeyAttribute.Name);
                        Console.WriteLine($"   - ForeignKey: {property.Name} -> {foreignKeyAttribute.Name}");
                    }
                }

                // Add the table schema to the snapshot (using pluralized table name)
                var tableName = entityType.Name.EndsWith("s") ? entityType.Name : $"{entityType.Name}s";
                snapshot.Tables[tableName] = tableSchema;
            }

            Console.WriteLine($"Snapshot Content: {SchemaSnapshot.Serialize(snapshot)}");
            return snapshot;
        }

        private Dictionary<string, object> GetDbSets()
        {
            var dbSets = new Dictionary<string, object>();

            var dbSetProperties = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                var propertyValue = property.GetValue(this);
                if (propertyValue != null)
                {
                    dbSets.Add(property.Name, propertyValue);
                }
            }

            return dbSets;
        }



        private SchemaSnapshot LoadPreviousSchemaSnapshot()
        {
            var json = File.ReadAllText(_snapshotPath);
            return JsonConvert.DeserializeObject<SchemaSnapshot>(json);
        }

        private void EnsureMigrationDirectoryExists()
        {
            if (!Directory.Exists(_migrationsDir))
            {
                Directory.CreateDirectory(_migrationsDir);
            }
        }
    }



}

