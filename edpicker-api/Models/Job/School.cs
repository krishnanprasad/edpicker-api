using System;

namespace edpicker_api.Models.Job
{
    public class School
    {
        public int SchoolId { get; set; }
        public string Name { get; set; }
        public string Logo { get; set; }
        public string Description { get; set; }
        public string Website { get; set; }
        public string City { get; set; }   // <-- Add this
        public string Board { get; set; }  // <-- Add this
                                           // Navigation property
        public ICollection<JobBoard> JobBoards { get; set; }
    }
}