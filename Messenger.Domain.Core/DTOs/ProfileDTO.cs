using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.DTOs
{
    public class ProfileDTO
    {
        public string Login { get; set; }
        public string? Nickname { get; set; }
        public string? Avatar { get; set; }
    }
}
