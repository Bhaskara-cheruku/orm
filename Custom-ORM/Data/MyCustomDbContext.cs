using Custom_ORM.Models;

namespace Custom_ORM.Data
{
    public class MyCustomDbContext:CustomDbContext
    {
        public MyCustomDbContext(string connectionString) : base(connectionString)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Orders> Orders { get; set; }
    }
}
