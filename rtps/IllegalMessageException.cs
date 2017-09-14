using System;
using System.Runtime.Serialization;

namespace rtps
{
    [Serializable]
    internal class IllegalMessageException : Exception
    {
        private object p;

        public IllegalMessageException()
        {
        }

        public IllegalMessageException(object p)
        {
            this.p = p;
        }

        public IllegalMessageException(string message) : base(message)
        {
        }

        public IllegalMessageException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected IllegalMessageException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}