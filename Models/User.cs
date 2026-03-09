using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class User
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        public string Password { get; set; }

        [Required]
        public string Role { get; set; } // Student, Lecturer, Administrator

        public DateTime CreatedDate { get; set; }

        public virtual Student Student { get; set; }
        public virtual Lecturer Lecturer { get; set; }
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
    }
}
