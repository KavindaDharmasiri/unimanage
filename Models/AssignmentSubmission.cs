using System;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class AssignmentSubmission
    {
        [Key]
        public int SubmissionId { get; set; }

        [Required]
        public int AssignmentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        public DateTime SubmissionDate { get; set; }

        public string SubmissionText { get; set; }

        public string FilePath { get; set; }

        public int? Grade { get; set; }

        public string Feedback { get; set; }

        public virtual Assignment Assignment { get; set; }
        public virtual Student Student { get; set; }
    }
}
