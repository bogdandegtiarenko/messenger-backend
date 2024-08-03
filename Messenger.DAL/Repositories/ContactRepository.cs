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
    public class ContactRepository : IContactRepository
    {
        private ApplicationDbContext _context;
        public ContactRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<RepositoryResponse> Create(Contact contact)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                await _context.Contacts.AddAsync(contact);
                await _context.SaveChangesAsync();
                repositoryResponse.Status = StatusCode.Success;
                repositoryResponse.IsSuccessCompleted = true;
                repositoryResponse.Message = "Successful addition";
                repositoryResponse.Data = contact.Id;
            }
            catch (Exception ex)
            {
                repositoryResponse.Status = StatusCode.Fail;
                repositoryResponse.IsSuccessCompleted = false;
                repositoryResponse.Message = ex.Message;
            }

            return repositoryResponse;
        }

        public IQueryable<Contact> GetAll()
        {
            return _context.Contacts;
        }

        public async Task<RepositoryResponse> Update(Contact contact)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Contacts.Update(contact);
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

        public async Task<RepositoryResponse> Delete(Contact contact)
        {
            RepositoryResponse repositoryResponse = new RepositoryResponse();
            try
            {
                _context.Contacts.Remove(contact);
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
