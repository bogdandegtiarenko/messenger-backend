using Messenger.Domain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.Models
{
    public class Message
    {
        public int Id { get; set; }
        public string Text { get; set; }

        public long? SenderId { get; set; }
        public User? Sender { get; set; }

        public long? RecipientId { get; set; }
        public User? Recipient { get; set; }

        public bool IsEdited { get; set; }
        public DateTime LastModificationDate { get; set; }
    }
}
