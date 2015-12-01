using System;
using System.Runtime.Serialization;

namespace Org.Kevoree.ModelGenerator
{
    [Serializable]
    class TypeDefinitionException : Exception
    {
        public TypeDefinitionException()
        {
        }

        public TypeDefinitionException(string message)
            : base(message)
        {
        }

        public TypeDefinitionException(string message, Exception inner)
            : base(message, inner)
        {
        }

        protected TypeDefinitionException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
