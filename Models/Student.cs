using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Student
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(20)]
        public string StudentNumber { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; }
    }
}
