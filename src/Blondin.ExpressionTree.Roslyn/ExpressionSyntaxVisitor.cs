using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Blondin.ExpressionTree.Roslyn
{
    public class ExpressionSyntaxVisitor
    {
        public virtual ExpressionSyntax Visit(Expression node)
        {
            switch (node)
            {
                case BinaryExpression binary:
                    return VisitBinary(binary);
                case ConstantExpression constant:
                    return VisitConstant(constant);
                case DefaultExpression @default:
                    return VisitDefault(@default);
                case MethodCallExpression methodCall:
                    return VisitMethodCall(methodCall);
                case ParameterExpression parameter:
                    return VisitParameter(parameter);
                default:
                    throw new NotSupportedException($"Expression of type {node.GetType()} is not supported.");
            }
        }

        protected virtual ExpressionSyntax VisitBinary(BinaryExpression node)
        {
            return BinaryExpression(
                node.NodeType.ToSyntaxKind(),
                Visit(node.Left),
                Visit(node.Right));
        }

        protected virtual ExpressionSyntax VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsEnum)
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    VisitType(node.Type),
                    IdentifierName(Enum.GetName(node.Type, node.Value)));
            }
            switch (node.Value)
            {
                case null:
                    return LiteralExpression(SyntaxKind.NullLiteralExpression);
                case char c:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(c));
                case decimal d:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d));
                case double d:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(d));
                case int i:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i));
                case float f:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(f));
                case long l:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(l));
                case string s:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(s));
                case uint i:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(i));
                case ulong l:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(l));
                case short s:
                    return LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(s));
                default:
                    throw new NotSupportedException($"Object type {node.Value?.GetType()} is not supported.");
            }
        }

        protected virtual ExpressionSyntax VisitDefault(DefaultExpression node)
        {
            return DefaultExpression(
                VisitType(node.Type));
        }

        protected virtual ExpressionSyntax VisitMethodCall(MethodCallExpression node)
        {
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    node.Object == null ? VisitType(node.Method.ReflectedType) : Visit(node.Object),
                    IdentifierName(node.Method.Name)))
                .WithArgumentList(
                    ArgumentList(
                        SeparatedList(
                            node.Arguments.Select(a => Argument(Visit(a))),
                            Enumerable.Repeat(
                                Token(SyntaxKind.CommaToken),
                                node.Arguments.Count - 1))));
        }

        static readonly Dictionary<Type, SyntaxKind> _predefinedTypes = new Dictionary<Type, SyntaxKind>
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
        protected TypeSyntax VisitType(Type type)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return NullableType(
                    VisitType(type.GetGenericArguments()[0]));
            }
            if (type.IsPointer)
            {
                return PointerType(
                    VisitType(type.GetElementType()));
            }
            if (_predefinedTypes.TryGetValue(type, out var kind))
            {
                return PredefinedType(Token(kind));
            }
            if (type.IsGenericType)
            {
                var genericArgs = type.GetGenericArguments();
                return GenericName(type.Name.Substring(0, type.Name.IndexOf('`')))
                    .WithTypeArgumentList(
                        TypeArgumentList(
                            SeparatedList(
                                genericArgs.Select(a => VisitType(a)),
                                Enumerable.Repeat(
                                    Token(SyntaxKind.CommaToken),
                                    genericArgs.Length - 1))));
            }
            return IdentifierName(type.Name);
        }

        protected virtual ExpressionSyntax VisitParameter(ParameterExpression node)
        {
            return IdentifierName(node.Name);
        }
    }
}
