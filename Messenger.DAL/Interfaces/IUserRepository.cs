using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Domain.Core.Models;


namespace Messenger.DAL.Interfaces
{
    public interface IUserRepository: IBaseRepository<User>
    {
    }
}
