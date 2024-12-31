namespace Custom_ORM.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int phone { get; set; }
        public DateTime DateOfBirth { get; set; }
        public Product product { get; set; }
    }
}
