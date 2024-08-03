using Messenger.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Messenger.Domain.Core;
using System.Text.Json;
using Messenger.Domain.Core.Responses;
using Microsoft.EntityFrameworkCore;
using Messenger.Domain.Core.Models;

namespace Messenger.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private ApplicationDbContext _context;
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RepositoryResponse> Create(User user)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                await _context.Users.AddAsync(user);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful addition";
                repositoryResponse.Data = user.Id;
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }

        public IQueryable<User> GetAll()
        {
            return _context.Users;
        }

        public async Task<RepositoryResponse> Update(User user)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Users.Update(user);
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

        public async Task<RepositoryResponse> Delete(User user)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Users.Remove(user);
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
