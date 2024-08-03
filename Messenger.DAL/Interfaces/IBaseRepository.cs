using Messenger.Domain.Core;
using Messenger.Domain.Core.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Messenger.DAL.Interfaces
{
    public interface IBaseRepository<T>
    {
        Task<RepositoryResponse> Create(T entity);
        IQueryable<T> GetAll();
        Task<RepositoryResponse> Update(T entity);
        Task<RepositoryResponse> Delete(T entity);
    }
}
