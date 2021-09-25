using System.Collections.Generic;

namespace PavlovRconWebserver.Models
{
    public class ValueFieldPartialViewViewModel
    {
        public List<Command> PlayerCommands { get; set; } = new List<Command>();
        public List<ExtendedCommand> TwoValueCommands { get; set; } = new List<ExtendedCommand>();
        public string ActualCommandName { get; set; }
        public bool IsNormalCommand { get; set; }
        public bool firstValue { get; set; }
    }
}