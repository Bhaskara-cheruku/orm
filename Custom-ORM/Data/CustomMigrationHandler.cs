using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

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
                _context.ExecuteSql(script);
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

            if (clrType.IsGenericType && clrType.GetGenericTypeDefinition() == typeof(Nullable<>))
                return MapClrTypeToSqlType(Nullable.GetUnderlyingType(clrType));

            return "VARCHAR(255)";
        }


        private string CompareSchemasAndGenerateSql(SchemaSnapshot previousSnapshot, SchemaSnapshot currentSnapshot)
        {
            var script = "-- SQL Migration Script\n";
            Console.WriteLine("Entered-hellooo");

            foreach (var currentTable in currentSnapshot.Tables)
            {
                Console.WriteLine(currentTable.Key);
                var tableName = currentTable.Key;
                var currentColumns = currentTable.Value.Columns;
                var previousColumns = previousSnapshot.Tables.ContainsKey(tableName)
                    ? previousSnapshot.Tables[tableName].Columns
                    : new List<string>();

                // Added and removed columns
                var addedColumns = currentColumns.Except(previousColumns).ToList();
                var removedColumns = previousColumns.Except(currentColumns).ToList();

                // Detect changed columns
                Console.WriteLine("Columns....");
                var changedColumns = currentColumns
                    .Where(c => previousColumns.Any(p => p.StartsWith(c.Split(' ')[0]) && p.Split(' ')[1] != c.Split(' ')[1]))
                    .ToList();
                if (changedColumns == null)
                {
                    Console.WriteLine("Null");
                }
                List<string> changedcolumnnames = new List<string>();
                foreach (var column in changedColumns)
                {
                    string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    changedcolumnnames.Add(parts[0]);
                    Console.WriteLine("changed column -"+column);
                    //string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    script += $"ALTER TABLE {tableName} ALTER COLUMN {parts[0]} {parts[1]};\n";
                }

                // Handle added columns
                foreach (var column in addedColumns)
                {
                    Console.WriteLine("Added Column ---" + column);
                    // Example column string: "ColumnName DataType"
                    string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    

                    if (parts.Length < 2)
                    {
                        throw new ArgumentException($"Invalid column definition: {column}");
                    }
                    string columnName = parts[0];
                    string dataType = parts[1]; 

                    string additionalAttributes = string.Join(" ", parts.Skip(2));

                    Console.WriteLine(columnName);

                    string[] validTypes = { "INT", "VARCHAR(MAX)", "DATETIME", "VARCHAR(255)", "DECIMAL(18, 2)"}; // Add all valid types
                    if (!validTypes.Contains(dataType.ToUpper()))
                    {
                        throw new ArgumentException($"Unsupported column type: {dataType}");
                    }

                    foreach (var changedcolumn in changedcolumnnames)
                    {
                        if (changedcolumn != columnName)
                        {
                            script += $"ALTER TABLE {tableName} ADD {columnName} {dataType} {additionalAttributes};\n";

                        }

                    }
                    if (changedcolumnnames.Count == 0)
                    {
                        script += $"ALTER TABLE {tableName} ADD {columnName} {dataType} {additionalAttributes};\n";

                    }



                }

                // Handle removed columns
                foreach (var column in removedColumns)
                {
                    Console.WriteLine("removed column -" + column);
                    // Example column string: "ColumnName DataType"
                    
                    string[] parts = column.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    

                    if (parts.Length < 2)
                    {
                        // Handle invalid column definitions
                        throw new ArgumentException($"Invalid column definition: {column}");
                    }

                    string columnName = parts[0]; // The first part is the column name

                    foreach (var changedcolumn in changedcolumnnames)
                    {
                        if (changedcolumn != columnName) {
                            script += $"ALTER TABLE {tableName} DROP COLUMN {columnName};\n";

                        }

                    }
                    if (changedcolumnnames.Count == 0)
                    {
                        script += $"ALTER TABLE {tableName} DROP COLUMN {columnName};\n";

                    }
                    // If the column is being removed, generate the DROP COLUMN SQL statement
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

        //private Dictionary<string, object> GetDbSets()
        //{
        //    var dbSets = new Dictionary<string, object>();

        //    var dbSetProperties = this.GetType()
        //        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        //        .Where(p => p.PropertyType.IsGenericType &&
        //                    p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

        //    foreach (var property in dbSetProperties)
        //    {
        //        var propertyValue = property.GetValue(this);
        //        if (propertyValue != null)
        //        {
        //            dbSets.Add(property.Name, propertyValue);
        //        }
        //    }

        //    return dbSets;
        //}



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

