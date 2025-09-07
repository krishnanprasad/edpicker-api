using System;

namespace edpicker_api.Models.QuestionPaper.Dto
{
    public class SchoolDto
    {
        public int SchoolId { get; set; }
        public string SchoolCode { get; set; }
        public string Name { get; set; }
        public string PrimaryEmail { get; set; }
        public byte Status { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime? UpdatedAtUtc { get; set; }
    }
}