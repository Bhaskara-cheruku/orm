using Custom_ORM.Models;
using System.Reflection;

namespace Custom_ORM.Data
{
    public class MyCustomDbContext:CustomDbContext
    {
        public MyCustomDbContext(string connectionString) : base(connectionString)
        {
            InitializeDbSets();
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }

        private void InitializeDbSets()
        {
            var dbSetProperties = this.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.PropertyType.IsGenericType &&
                            p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            foreach (var property in dbSetProperties)
            {
                var entityType = property.PropertyType.GetGenericArguments().First();
                var dbSetInstance = Activator.CreateInstance(
                    typeof(DbSet<>).MakeGenericType(entityType), this);
                property.SetValue(this, dbSetInstance);
            }
        }
    }
}
