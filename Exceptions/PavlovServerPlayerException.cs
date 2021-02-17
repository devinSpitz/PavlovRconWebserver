using System;

namespace PavlovRconWebserver.Exceptions
{
    
    [Serializable]
    public class PavlovServerPlayerException : Exception
    {

        
        public string FieldName { get; set; }

        public PavlovServerPlayerException()
        {
        }

        
        public PavlovServerPlayerException(string fieldName,string message) : base(message)
        {
            FieldName = fieldName;
        }
        
        public PavlovServerPlayerException(string message) : base(message)
        {
        }

        public PavlovServerPlayerException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}