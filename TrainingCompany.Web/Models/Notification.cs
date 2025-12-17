using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class Notification
    {
        public int NotificationID { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public string Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}