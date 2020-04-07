using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using Blondin.ExpressionTree.Roslyn.Mono;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NUnit.Framework;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Blondin.ExpressionTree.Roslyn.Tests
{
    public class ExtensionVisitTests
    {
        [Test]
        public void ForLoop()
        {
            // Arrange
            var i = Expression.Parameter(typeof(int), "i");
            var j = Expression.Parameter(typeof(int), "j");
            var loop = new ForExpression(
                i,
                Expression.Constant(0),
                Expression.LessThan(i, Expression.Constant(10)),
                Expression.Increment(i),
                Expression.Increment(j),
                null,
                null);

            // Act
            var sut = new ForExpressionSyntaxVisitor();
            var visited = sut.Visit(loop).NormalizeWhitespace();

            // Assert
            Assert.That(visited.ToString(), Is.EqualTo(
@"for (0; i < 10; i++)
{
    j++;
}"));
        }

        private class ForExpressionSyntaxVisitor : ExpressionSyntaxVisitor
        {
            protected override CSharpSyntaxNode VisitExtension(Expression node)
            {
                switch (node)
                {
                    case ForExpression @for:
                        return ForStatement(
                            Peel<BlockSyntax>(Visit(@for.Body)))
                            .WithInitializers(
                                SingletonSeparatedList(
                                    Peel<ExpressionSyntax>(Visit(@for.Initializer))))
                            .WithCondition(
                                Peel<ExpressionSyntax>(Visit(@for.Test)))
                            .WithIncrementors(
                                SingletonSeparatedList(
                                    Peel<ExpressionSyntax>(Visit(@for.Step))));
                    default:
                        return base.VisitExtension(node);
                }
            }
        }
    }
}
