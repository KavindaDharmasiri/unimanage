using System;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class CourseMaterial
    {
        [Key]
        public int MaterialId { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        public string Description { get; set; }

        public string FilePath { get; set; }

        public DateTime UploadDate { get; set; }

        public virtual Course Course { get; set; }
    }
}
