using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Domain.Core.Models;

namespace Messenger.Domain.Core.DTOs
{
    public class ContactBlockDTO
    {
        public ProfileDTO Profile { get; set; }
        public ContactMessageInfo MessageInfo { get; set; }
    }
}
