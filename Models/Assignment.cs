using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Assignment
    {
        public int AssignmentId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        [Required]
        public int MaxMarks { get; set; }

        public string FilePath { get; set; }

        public virtual Course Course { get; set; }
        public virtual ICollection<AssignmentSubmission> Submissions { get; set; }
    }
}
