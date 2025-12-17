using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class Course
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int CategoryID { get; set; }
        public string CategoryName { get; set; }
        public decimal Price { get; set; }
        public string Schedule { get; set; }
        public string Mode { get; set; }
        public string Duration { get; set; }
        public string ImageURL { get; set; }
    }
}