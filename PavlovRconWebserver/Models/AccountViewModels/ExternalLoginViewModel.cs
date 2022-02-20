using System.ComponentModel.DataAnnotations;

namespace PavlovRconWebserver.Models.AccountViewModels
{
    public class ExternalLoginViewModel
    {
        [Required] public string UserName { get; set; }
    }
}