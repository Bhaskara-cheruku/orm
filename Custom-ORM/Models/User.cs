using System.ComponentModel.DataAnnotations.Schema;

namespace Custom_ORM.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        //public int phone { get; set; }
        //public int Status { get; set; } 
        //[ForeignKey("Product")]
        //public int productId {  get; set; }
        //[ForeignKey("Order")]
        //public int orderId { get; set; }
    }
}
