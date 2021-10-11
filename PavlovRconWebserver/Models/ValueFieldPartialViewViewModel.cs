using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class ValueFieldPartialViewViewModel
    {
        public List<Command> PlayerCommands { get; set; } = new();
        public List<ExtendedCommand> TwoValueCommands { get; set; } = new();
        public string ActualCommandName { get; set; }
        public bool IsNormalCommand { get; set; }
        public bool firstValue { get; set; }
    }
}