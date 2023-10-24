using IdentityMessageBoard.DataAccess;
using IdentityMessageBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using System.Data;

namespace IdentityMessageBoard.Controllers
{
    public class MessagesController : Controller
    {
        private readonly MessageBoardContext _context;

        public MessagesController(MessageBoardContext context)
        {
            _context = context;
        }


        public IActionResult Index()
        {
            var messages = _context.Messages
                .Include(m => m.Author)
                .OrderBy(m => m.ExpirationDate)
                .ToList()
                .Where(m => m.IsActive()); // LINQ Where(), not EF Where()

            return View(messages);
        }

        public IActionResult AllMessages()
        {
            var allMessages = GroupMessages(_context.Messages);
            return View(allMessages);
        }

        [Authorize]
        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public IActionResult Create(string userId, string content, int expiresIn)
        {
            var user = _context.ApplicationUsers.Find(userId);

            try
            {
                _context.Messages.Add(
                    new Message()
                    {
                        Content = content,
                        ExpirationDate = DateTime.UtcNow.AddDays(expiresIn),
                        Author = user
                    });

                _context.SaveChanges();

            }
            catch (Exception ex)
            {
                Log.Warning("Message failed to save to the DB" + ex);
            }

            return RedirectToAction("Index");
        }

        private Dictionary<string, List<Message>> GroupMessages(IEnumerable<Message> messages)
        {
            var allMessages = new Dictionary<string, List<Message>>()
            {
                { "active" , new List<Message>() },
                { "expired", new List<Message>() }
            };

            foreach (var message in _context.Messages)
            {
                if (message.IsActive())
                {
                    allMessages["active"].Add(message);
                }
                else
                {
                    allMessages["expired"].Add(message);
                }
            }

            return allMessages;
        }
    }
}
