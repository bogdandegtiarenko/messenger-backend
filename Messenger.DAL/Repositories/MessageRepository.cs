using Messenger.DAL.Interfaces;
using Messenger.Domain.Core;
using Messenger.Domain.Core.Models;
using Messenger.Domain.Core.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Messenger.DAL.Repositories
{
    public class MessageRepository: IMessageRepository
    {
        private ApplicationDbContext _context;
        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RepositoryResponse> Create(Message message)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                await _context.Messages.AddAsync(message);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful addition";
                repositoryResponse.Data = message.Id;
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }

        public IQueryable<Message> GetAll()
        {
            return _context.Messages;
        }

        public async Task<RepositoryResponse> Update(Message message)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Messages.Update(message);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful updated";
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }

        public async Task<RepositoryResponse> Delete(Message message)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Messages.Remove(message);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful deleted";
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }
    }
}
