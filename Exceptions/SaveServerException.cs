using System;

namespace PavlovRconWebserver.Exceptions
{
    [Serializable]
    public class SaveServerException : Exception
    {
        public SaveServerException()
        {
        }


        public SaveServerException(string fieldName, string message) : base(message)
        {
            FieldName = fieldName;
        }

        public SaveServerException(string message) : base(message)
        {
        }

        public SaveServerException(string message, Exception innerException) : base(message, innerException)
        {
        }


        public string FieldName { get; set; }
    }
}