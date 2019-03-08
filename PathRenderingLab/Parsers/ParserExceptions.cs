using System;
using System.Runtime.Serialization;

namespace PathRenderingLab.Parsers
{
    [Serializable]
    public class ParserException : Exception
    {
        public ParserException() { }
        public ParserException(string message) : base(message) { }
        public ParserException(string message, Exception inner) : base(message, inner) { }
        protected ParserException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    internal class EndParseException : Exception
    {
        public EndParseException() { }
        public EndParseException(string message) : base(message) { }
        public EndParseException(string message, Exception innerException) : base(message, innerException) { }
        protected EndParseException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
