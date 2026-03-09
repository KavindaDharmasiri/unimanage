using System;
using System.ComponentModel.DataAnnotations;

namespace uniManage.Models
{
    public class Message
    {
        public int MessageId { get; set; }

        [Required]
        public int SenderId { get; set; }

        [Required]
        public int ReceiverId { get; set; }

        [Required]
        public string Subject { get; set; }

        [Required]
        public string Body { get; set; }

        public DateTime SentDate { get; set; }

        public bool IsRead { get; set; }

        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }
}
