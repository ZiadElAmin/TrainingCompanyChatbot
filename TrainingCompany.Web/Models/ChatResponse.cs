using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TrainingCompany.Web.Models
{
    public class ChatResponse
    {
        public bool Success { get; set; }
        public string Response { get; set; }
        public string Intent { get; set; }
        public decimal Confidence { get; set; }
        public string ActionType { get; set; }
    }
}