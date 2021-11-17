namespace PavlovRconWebserver.Models
{
    public class ServerInfoViewModel
    {
        public ServerInfo ServerInfo { get; set; }
        public int ServerId { get; set; }
        public string Name { get; set; }
        public string AdditionalHtml { get; set; } = "";
    }

    public class ServerInfo
    {
        public string MapLabel { get; set; }
        public string MapPictureLink { get; set; }
        public string ShowImage()
        {
            if (string.IsNullOrEmpty(MapPictureLink))
            {
                return "/images/noImg.png";
            }
            else
            {
                return MapPictureLink;
            }
        }
        public string GameMode { get; set; }
        public string ServerName { get; set; }

        /// <summary>
        ///     can be StandBy && Ended && Started maybe more
        /// </summary>
        public string RoundState { get; set; } // 

        public string PlayerCount { get; set; }
        public string Teams { get; set; }
        public string Team0Score { get; set; }
        public string Team1Score { get; set; }
    }
}