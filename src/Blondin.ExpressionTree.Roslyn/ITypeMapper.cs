using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;

namespace Blondin.ExpressionTree.Roslyn
{
    public interface ITypeMapper
    {
        (TypeSyntax Syntax, IEnumerable<NameSyntax> Namespaces) MapType(Type type);

        (IEnumerable<TypeSyntax> Syntax, IEnumerable<NameSyntax> Namespaces) MapTypes(IEnumerable<Type> types);
    }
}