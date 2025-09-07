using System;
using System.ComponentModel.DataAnnotations;

namespace edpicker_api.Models.Job
{
    public class JobBoardContactDetails
    {
        [Key]
        public int ContactId { get; set; }
        public string ContactName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }

        // Navigation property
        public ICollection<JobBoard> JobBoards { get; set; }
    }
}