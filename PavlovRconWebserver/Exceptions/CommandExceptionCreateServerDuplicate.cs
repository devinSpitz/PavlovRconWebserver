using System;

namespace PavlovRconWebserver.Exceptions
{
    [Serializable]
    public class CommandExceptionCreateServerDuplicate : Exception
    {
        public CommandExceptionCreateServerDuplicate()
        {
        }

        public CommandExceptionCreateServerDuplicate(string message) : base(message)
        {
        }

        public CommandExceptionCreateServerDuplicate(string message, Exception innerException) : base(message,
            innerException)
        {
        }
    }
}