using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class CartItem
    {
        public int CartID { get; set; }
        public int CourseID { get; set; }
        public string Title { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; }
    }
}