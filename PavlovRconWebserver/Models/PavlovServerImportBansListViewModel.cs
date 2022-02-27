using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PavlovRconWebserver.Models
{

    public class PavlovServerImportBansListViewModel
    {
        public int ServerId { get; set; } = 0;
        
        [DisplayName("Bans")]
        [Display(Description = "Upload your file list:")]
        public IFormFile BansFromFile { get; set; }
    }
}