using IdentityMessageBoard.DataAccess;
using IdentityMessageBoard.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Serilog;
using System.Data;

namespace IdentityMessageBoard.Controllers
{
    //[Authorize(Policy = "AuthenticatedUser")]
    //[Authorize(Roles = "Admin")]
    public class MessagesController : Controller
    {
        private readonly MessageBoardContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MessagesController(MessageBoardContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize]
        public IActionResult Index(string userId)
        {
            var user = _userManager.FindByIdAsync(userId).Result;
            var isSuperUser = User.IsInRole("SuperUser");

            var messages = _context.Messages
                .Include(m => m.Author)
                .OrderBy(m => m.ExpirationDate)
                .ToList()
                .Where(m => m.IsActive()); // LINQ Where(), not EF Where()

            if (isSuperUser)
            {
                return RedirectToAction("SuperUserIndex");
            }

            return View(messages);
        }

        [Authorize(Roles = "Admin,SuperUser")]
        public IActionResult SuperUserIndex()
        {
            var allMessages = GroupMessages(_context.Messages);
            return View(allMessages);
        }

        [Authorize(Roles = "Admin,SuperUser")]
        [Route("users/{userId:int}/messages/{messageId:int}/edit")]
        public IActionResult Edit()
        {
            return View();
        }

        /*
        [Authorize(Roles = "Admin,SuperUser")]
        [Route("users/{userId:int}/messages/{messageId:int}")]
        public IActionResult Update(int messageId, string content)
        {
            var message = _context.Messages.Find(messageId);
            if (message == null) return NotFound();

            message.Id = messageId;
        }
        */

        //[Authorize(Policy = "AdminOnly")]
        [Authorize(Roles = "Admin")]
        public IActionResult AllMessages()
        {
            var allMessages = GroupMessages(_context.Messages);
            return View(allMessages);
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
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
