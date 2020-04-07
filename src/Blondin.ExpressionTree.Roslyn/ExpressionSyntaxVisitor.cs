using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Blondin.ExpressionTree.Roslyn
{
    public class ExpressionSyntaxVisitor
    {
        private readonly IList<NameSyntax> _namespaces = new List<NameSyntax>();
        private readonly ITypeMapper _typeMapper;

        public ExpressionSyntaxVisitor(ClassDeclarationSyntax @class = null, ITypeMapper typeMapper = null)
        {
            Class = @class;
            _typeMapper = typeMapper ?? new DefaultTypeMapper();
        }

        public ClassDeclarationSyntax Class { get; private set; }

        public IEnumerable<UsingDirectiveSyntax> Namespaces =>
            _namespaces
            .OrderBy(n => n.ToString())
            .Select(n => UsingDirective(n));

        public virtual CSharpSyntaxNode Visit(Expression node)
        {
            switch (node)
            {
                case UnaryExpression unary:
                    return VisitUnary(unary);
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
                case LambdaExpression lambda:
                    return VisitLambda(lambda);
                case BlockExpression block:
                    return VisitBlock(block);
                case NewExpression @new:
                    return VisitNew(@new);
                case Expression extension when extension.NodeType == ExpressionType.Extension:
                    return VisitExtension(extension);
                default:
                    throw new NotSupportedException($"Expression of type {node.GetType()} is not supported.");
            }
        }

        public virtual IEnumerable<CSharpSyntaxNode> VisitNodes(IEnumerable<Expression> nodes)
        {
            foreach (var node in nodes)
            {
                yield return Visit(node);
            }
        }

        protected virtual CSharpSyntaxNode VisitLambda(LambdaExpression lambda)
        {
            var parameters = from p in lambda.Parameters
                             select Parameter(Identifier(p.Name))
                                    .WithType(MapType(p.Type));
            if (lambda.Name != null && Class != null && !ClosureVisitor.VisitExpression(lambda).CapturedVariables.Any())
            {
                var signature = lambda.Type.GetMethod("Invoke");
                var method = MethodDeclaration(
                    MapType(signature.ReturnType),
                        Identifier(lambda.Name))
                    .WithModifiers(
                        TokenList(
                            Token(SyntaxKind.PublicKeyword),
                            Token(SyntaxKind.StaticKeyword)))
                    .WithParameterList(ParameterList(SeparatedList(parameters)))
                    .WithBody(
                        Peel<BlockSyntax>(Visit(lambda.Body), valueReturnedExpected: signature.ReturnType != typeof(void)));
                Class = Class.AddMembers(method);
                return method;
            }
            return ObjectCreationExpression(MapType(lambda.Type))
                .WithArgumentList(
                    ArgumentList(
                        SingletonSeparatedList(
                            Argument(
                                ParenthesizedLambdaExpression(
                                    ParameterList(SeparatedList(parameters)),
                                    Visit(lambda.Body))))));
        }

        protected virtual BlockSyntax VisitBlock(BlockExpression node)
        {
            var declarations = node.Variables.Select(v =>
                (StatementSyntax)LocalDeclarationStatement(
                    VariableDeclaration(MapType(node.Type))
                    .WithVariables(
                        SingletonSeparatedList(
                            VariableDeclarator(
                                Identifier(v.Name ?? throw new InvalidOperationException("Variable declared without a name.")))))));
            var statements = VisitNodes(node.Expressions)
                .Select((e, i) => node.Type == typeof(void) || i < node.Expressions.Count - 1 ?
                    Peel<StatementSyntax>(e) :
                    Peel<ReturnStatementSyntax>(e));
            return Block(declarations.Concat(statements));
        }

        protected virtual CSharpSyntaxNode VisitUnary(UnaryExpression node)
        {
            return PostfixUnaryExpression(
                node.NodeType.ToSyntaxKind(),
                Peel<ExpressionSyntax>(Visit(node.Operand)));
        }

        protected virtual CSharpSyntaxNode VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Assign:
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked:
                case ExpressionType.AndAssign:
                case ExpressionType.DivideAssign:
                case ExpressionType.ExclusiveOrAssign:
                case ExpressionType.LeftShiftAssign:
                case ExpressionType.ModuloAssign:
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked:
                case ExpressionType.OrAssign:
                case ExpressionType.PostDecrementAssign:
                case ExpressionType.PostIncrementAssign:
                case ExpressionType.PowerAssign:
                case ExpressionType.RightShiftAssign:
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked:
                    return AssignmentExpression(
                        node.NodeType.ToSyntaxKind(),
                        Peel<ExpressionSyntax>(Visit(node.Left)),
                        Peel<ExpressionSyntax>(Visit(node.Right)));
                default:
                    return BinaryExpression(
                        node.NodeType.ToSyntaxKind(),
                        Peel<ExpressionSyntax>(Visit(node.Left)),
                        Peel<ExpressionSyntax>(Visit(node.Right)));
            }
        }

        protected T Peel<T>(CSharpSyntaxNode node, bool valueReturnedExpected = false)
            where T : CSharpSyntaxNode
        {
            if (node == null)
            {
                return null;
            }
            if (node is T asT)
            {
                return asT;
            }
            if (typeof(T) == typeof(StatementSyntax))
            {
                return (T)(object)ExpressionStatement((ExpressionSyntax)node);
            }
            if (typeof(T) == typeof(ReturnStatementSyntax))
            {
                return (T)(object)ReturnStatement((ExpressionSyntax)node);
            }
            if (typeof(T) == typeof(BlockSyntax))
            {
                return (T)(object)Block(
                    valueReturnedExpected ?
                    Peel<ReturnStatementSyntax>(node) :
                    Peel<StatementSyntax>(node));
            }
            return node as T ?? throw new InvalidOperationException($"Node cannot be converted into a {typeof(T)}.");
        }

        protected virtual CSharpSyntaxNode VisitConstant(ConstantExpression node)
        {
            if (node.Type.IsEnum)
            {
                return MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    MapType(node.Type),
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

        protected virtual CSharpSyntaxNode VisitDefault(DefaultExpression node)
        {
            return DefaultExpression(MapType(node.Type));
        }

        protected virtual CSharpSyntaxNode VisitMethodCall(MethodCallExpression node)
        {
            var instance = node.Object == null ? MapType(node.Method.ReflectedType) : Peel<ExpressionSyntax>(Visit(node.Object));
            var arguments = SeparatedList(
                node.Arguments.Select(a => Argument(Peel<ExpressionSyntax>(Visit(a)))));
            return InvocationExpression(
                MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                        instance,
                        IdentifierName(node.Method.Name)))
                            .WithArgumentList(
                                ArgumentList(arguments));
        }

        protected virtual CSharpSyntaxNode VisitNew(NewExpression node)
        {
            var arguments = SeparatedList(
                node.Arguments.Select(a => Argument(Peel<ExpressionSyntax>(Visit(a)))));
            return ObjectCreationExpression(
                MapType(node.Type))
                .WithArgumentList(
                    ArgumentList(arguments));
        }

        protected virtual ExpressionSyntax VisitParameter(ParameterExpression node)
        {
            return IdentifierName(node.Name);
        }

        protected virtual CSharpSyntaxNode VisitExtension(Expression node)
        {
            return Visit(node.Reduce());
        }

        protected TypeSyntax MapType(Type type)
        {
            var (result, namespaces) = _typeMapper.MapType(type);
            AddNamespaces(namespaces);
            return result;
        }

        private void AddNamespaces(IEnumerable<NameSyntax> namespaces)
        {
            foreach (var @namespace in namespaces)
            {
                if (!_namespaces.Any(n => n.IsEquivalentTo(@namespace)))
                {
                    _namespaces.Add(@namespace);
                }
            }
        }
    }
}
