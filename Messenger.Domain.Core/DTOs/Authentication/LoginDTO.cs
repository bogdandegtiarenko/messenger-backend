using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.DTOs.Authentication
{
    public class LoginDTO
    {
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
