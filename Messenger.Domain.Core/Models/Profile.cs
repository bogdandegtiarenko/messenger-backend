using Messenger.Domain.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.Models
{
    public class Profile
    {
        public int Id { get; set; }
        public string Nickname { get; set; }
        public byte[]? Avatar { get; set; }

        public User User { get; set; }
    }
}
