using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Blondin.ExpressionTree.Roslyn
{
    public class DefaultTypeMapper : ITypeMapper
    {
        private static readonly Dictionary<Type, SyntaxKind> _predefinedTypes = new Dictionary<Type, SyntaxKind>
        {
            [typeof(string)] = SyntaxKind.StringKeyword,
            [typeof(object)] = SyntaxKind.ObjectKeyword,
            [typeof(Enum)] = SyntaxKind.EnumKeyword,
            [typeof(Delegate)] = SyntaxKind.DelegateKeyword,
            [typeof(void)] = SyntaxKind.VoidKeyword,
            [typeof(bool)] = SyntaxKind.BoolKeyword,
            [typeof(char)] = SyntaxKind.CharKeyword,
            [typeof(sbyte)] = SyntaxKind.SByteKeyword,
            [typeof(byte)] = SyntaxKind.ByteKeyword,
            [typeof(short)] = SyntaxKind.ShortKeyword,
            [typeof(ushort)] = SyntaxKind.UShortKeyword,
            [typeof(int)] = SyntaxKind.IntKeyword,
            [typeof(uint)] = SyntaxKind.UIntKeyword,
            [typeof(long)] = SyntaxKind.LongKeyword,
            [typeof(ulong)] = SyntaxKind.ULongKeyword,
            [typeof(decimal)] = SyntaxKind.DecimalKeyword,
            [typeof(float)] = SyntaxKind.FloatKeyword,
            [typeof(double)] = SyntaxKind.DoubleKeyword,
            [typeof(string)] = SyntaxKind.StringKeyword
        };

        public virtual (TypeSyntax Syntax, IEnumerable<NameSyntax> Namespaces) MapType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                var (elementType, namespaces) = MapType(type.GetGenericArguments()[0]);
                return (NullableType(elementType), namespaces);
            }
            if (type.IsPointer)
            {
                var (elementType, namespaces) = MapType(type.GetElementType());
                return (PointerType(elementType), namespaces);
            }
            if (_predefinedTypes.TryGetValue(type, out var kind))
            {
                return (PredefinedType(Token(kind)), Enumerable.Empty<NameSyntax>());
            }
            return type.IsGenericType ?
                MapGenericType(type) :
                (IdentifierName(type.Name), Enumerable.Repeat(ParseName(type.Namespace), 1));
        }

        private (TypeSyntax Syntax, IEnumerable<NameSyntax> Namespaces) MapGenericType(Type type)
        {
            var (genericArgs, typeNamespaces) = MapTypes(type.GetGenericArguments());
            var namespaces = new List<NameSyntax>(typeNamespaces);
            var @namespace = ParseName(type.Namespace);
            if (!namespaces.Any(n => n.IsEquivalentTo(@namespace)))
            {
                namespaces.Add(@namespace);
            }
            return (GenericName(type.Name.Substring(0, type.Name.IndexOf('`')))
                .WithTypeArgumentList(
                    TypeArgumentList(
                        SeparatedList(
                            genericArgs))),
                namespaces);
        }

        public (IEnumerable<TypeSyntax> Syntax, IEnumerable<NameSyntax> Namespaces) MapTypes(IEnumerable<Type> types)
        {
            var result = new List<TypeSyntax>();
            var namespaces = new List<NameSyntax>();
            foreach (var type in types)
            {
                var (syntax, typeNamespaces) = MapType(type);
                result.Add(syntax);

                foreach (var @namespace in typeNamespaces)
                {
                    if (!namespaces.Any(n => n.IsEquivalentTo(@namespace)))
                    {
                        namespaces.Add(@namespace);
                    }
                }
            }
            return (result, namespaces);
        }
    }
}
