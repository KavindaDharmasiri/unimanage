using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Course
    {
        public int CourseId { get; set; }

        [Required]
        [StringLength(10)]
        public string CourseCode { get; set; }

        [Required]
        [StringLength(200)]
        public string CourseName { get; set; }

        public string Description { get; set; }

        [Required]
        public int Credits { get; set; }

        public int? PrerequisiteCourseId { get; set; }

        [Required]
        public int MaxEnrollment { get; set; }

        public int? LecturerId { get; set; }

        public virtual Lecturer Lecturer { get; set; }
        public virtual Course PrerequisiteCourse { get; set; }
        public virtual ICollection<Enrollment> Enrollments { get; set; }
        public virtual ICollection<Assignment> Assignments { get; set; }
        public virtual ICollection<CourseMaterial> Materials { get; set; }
    }
}
