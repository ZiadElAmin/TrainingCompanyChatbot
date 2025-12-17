using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class CourseRecommendation
    {
        public int CourseID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Mode { get; set; }
    }
}