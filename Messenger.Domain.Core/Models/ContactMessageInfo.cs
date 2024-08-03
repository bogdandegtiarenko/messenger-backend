using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.Models
{
    public class ContactMessageInfo
    {
        public string LastMessage { get; set; }
        public DateTime LastMessageDateTime { get; set; }

        public int? UnreadMessageCount { get; set; }
    }
}
