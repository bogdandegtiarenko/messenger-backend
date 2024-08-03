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
    public class ProfileRepository : IProfileRepository
    {
        private ApplicationDbContext _context;
        public ProfileRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RepositoryResponse> Create(Profile profile)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                await _context.Profiles.AddAsync(profile);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful addition";
                repositoryResponse.Data = profile.Id;
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }

        public IQueryable<Profile> GetAll()
        {
            return _context.Profiles;
        }

        public async Task<RepositoryResponse> Update(Profile profile)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Profiles.Update(profile);
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

        public async Task<RepositoryResponse> Delete(Profile profile)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Profiles.Remove(profile);
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
