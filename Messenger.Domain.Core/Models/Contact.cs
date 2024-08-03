using Messenger.Domain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.Models
{
    public class Contact
    {
        public long Id { get; set; }

        public long? UserId { get; set; }
        public User? User { get; set; }

        public long? ContactUserId { get; set; }
        public User? ContactUser { get; set; }
    }
}
