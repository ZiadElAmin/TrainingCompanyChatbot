using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class User
    {
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsAdmin { get; set; }
    }
}