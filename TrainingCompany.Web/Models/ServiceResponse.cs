using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class ServiceResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string Data { get; set; }
    }
}