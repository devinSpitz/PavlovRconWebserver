using System;

namespace PavlovRconWebserver.Exceptions
{
    [Serializable]
    public class ValidateException : Exception
    {
        public ValidateException()
        {
        }

        public ValidateException(string fieldName, string message) : base(message)
        {
            FieldName = fieldName;
        }
        public ValidateException(string message) : base(message)
        {
        }

        public ValidateException(string message, Exception innerException) : base(message, innerException)
        {
        }
        
        public string FieldName { get; set; } = "";
    }
}