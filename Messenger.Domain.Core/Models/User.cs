using Messenger.Domain.Core;

namespace Messenger.Domain.Core.Models
{
    public class User
    {
        public long? Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }

        public int ProfileId { get; set; }
        public Profile Profile { get; set; }

        public List<Contact> UserContacts { get; set; } = new();
        public List<Contact> ContactContacts { get; set; } = new();


        public List<Message> SenderMessages { get; set; } = new();
        public List<Message> RecipientMessages { get; set; } = new();
    }
}