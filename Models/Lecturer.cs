using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Lecturer
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [StringLength(50)]
        public string Department { get; set; }

        public virtual User User { get; set; }
        public virtual ICollection<Course> Courses { get; set; }
    }
}
