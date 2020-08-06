using System.Threading.Tasks;

namespace PavlovRconWebserver.Services
{
   public interface IEmailSender
   {
      Task SendEmailAsync(string email, string subject, string message);
   }
}