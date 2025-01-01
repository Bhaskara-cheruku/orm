using System.ComponentModel.DataAnnotations.Schema;

namespace Custom_ORM.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string name { get; set; }
        public string discription {  get; set; }
        public string discount {  get; set; }  
        public int price { get; set; }


        [ForeignKey("Order")]
        public int orderId { get; set; }

    }
}
