using System;
using System.Collections.Generic;
using System.Text;

namespace Tiger.Parsers
{
    [Serializable()]
    public class InvalidTypeError : System.Exception
    {
        public InvalidTypeError() : base() { }
        public InvalidTypeError(string message) : base(message) { }
        public InvalidTypeError(string message, System.Exception inner) : base(message, inner) { }

        // A constructor is needed for serialization when an
        // exception propagates from a remoting server to the client.
        protected InvalidTypeError(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}
