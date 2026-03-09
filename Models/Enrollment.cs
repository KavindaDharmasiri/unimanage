using System;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Enrollment
    {
        public int EnrollmentId { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        public DateTime EnrollmentDate { get; set; }

        public string Status { get; set; } // Active, Completed, Dropped

        public virtual Student Student { get; set; }
        public virtual Course Course { get; set; }
    }
}
