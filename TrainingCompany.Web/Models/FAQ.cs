using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class FAQ
    {
        public int FAQID { get; set; }

        
        public string Question { get; set; }
        public string Answer { get; set; }
        public string IntentTag { get; set; }
        public string Keywords { get; set; }

        public string Category { get; set; }

        public int Priority { get; set; }
    }
}