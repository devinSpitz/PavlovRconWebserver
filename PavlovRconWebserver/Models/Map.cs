using System.ComponentModel.DataAnnotations.Schema;

namespace PavlovRconWebserver.Models
{
    public class Map
    {
        public string Id { get; set; }
        public string Name { get; set; } = "";
        public string Author { get; set; } = "";
        public string ImageUrl { get; set; } = "";

        public string showImage()
        {
            if (string.IsNullOrEmpty(ImageUrl))
            {
                return "/images/noImg.png";
            }
            else
            {
                return ImageUrl;
            }
        }

        [NotMapped] public int sort { get; set; } = 0;
    }
}