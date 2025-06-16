using System;

namespace edpicker_api.Models.Job
{
    public class JobBoard
    {
        public string Id { get; set; }
        public string Board { get; set; }
        public string City { get; set; }
        public bool Verified { get; set; }
        public decimal Salary { get; set; }
        public string UserCity { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
