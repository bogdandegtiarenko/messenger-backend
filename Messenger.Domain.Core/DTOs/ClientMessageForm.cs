using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.DTOs
{
    public class ClientMessageForm
    {
        public string SenderLogin { get; set; }
        public string RecipientLogin { get; set; }
        public string Text { get; set; }
        public string SenderConnectionId { get; set; }
    }
}
