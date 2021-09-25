namespace PavlovRconWebserver.Extensions
{
    public static class FilePaths
    {
        public static string BanList { get; } = "Pavlov/Saved/Config/blacklist.txt";
        public static string GameIni { get; } = "Pavlov/Saved/Config/LinuxServer/Game.ini";
        public static string WhiteList { get; } = "Pavlov/Saved/Config/whitelist.txt";
        public static string ModList { get; } = "Pavlov/Saved/Config/mods.txt";
        public static string RconSettings { get; } = "Pavlov/Saved/Config/RconSettings.txt";
    }
}