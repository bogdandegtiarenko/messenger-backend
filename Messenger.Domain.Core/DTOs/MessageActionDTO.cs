using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.DTOs
{
    public class MessageActionDTO
    {
        public int Id { get; set; }
        public DateTime OperationDate { get; set; }
    }
}
