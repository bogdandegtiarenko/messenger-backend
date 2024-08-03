using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.Domain.Core.Responses
{
    public class RepositoryResponse : IBaseResponse
    {
        public StatusCode Status { get; set; }
        public bool IsSuccessCompleted { get; set; }
        public string? Message { get; set; }
        public object? Data { get; set; }
    }
}
