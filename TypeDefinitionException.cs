using System;

namespace Org.Kevoree.ModelGenerator
{
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
    }
}
