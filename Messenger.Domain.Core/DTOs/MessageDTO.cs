using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.DTOs
{
    public class MessageDTO
    {
        public long Id { get; set; }
        public string SenderLogin { get; set; }
        public string RecipientLogin { get; set; }
        public string Text { get; set; }
        public DateTime LastModify { get; set; }
    }
}
