using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Blondin.ExpressionTree.Roslyn.Tests
{
    public class SimpleTests
    {
        [Test]
        [TestCaseSource(typeof(SimpleTests), nameof(TestCases))]
        public void SimpleExpressions(ExpressionSyntaxVisitor sut, Expression expression, string expected)
        {
            // Act
            var result = sut.Visit(expression).NormalizeWhitespace();
            // Assert
            Assert.That(result.ToString(), Is.EqualTo(expected));
        }

        public static IEnumerable TestCases => new[]
        {
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((int i) => 1 + i),
                "new Func<int, int>((int i) => 1 + i)").SetName("(int i) > 1 + i"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => "a" + s).Body,
                @"""a"" + s").SetName(@"""a"" + s"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => s.Split('c', StringSplitOptions.RemoveEmptyEntries)).Body,
                @"s.Split('c', StringSplitOptions.RemoveEmptyEntries)").SetName(@"s_Split('c', StringSplitOptions_RemoveEmptyEntries)"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => string.Intern(s)).Body,
                @"string.Intern(s)").SetName(@"string_Intern(s)"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                Expression.Default(typeof(KeyValuePair<string, string>)),
                @"default(KeyValuePair<string, string>)").SetName(@"default(KeyValuePair<string, string>)"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                Expression.New(
                    typeof(DateTime).GetConstructor(new[] { typeof(long) }),
                    Expression.Constant(42L)),
                @"new DateTime(42L)").SetName(@"new DateTime(42L)"),
        };

        private static Expression<Func<T, TResult>> CreateExpression<T, TResult>(Expression<Func<T, TResult>> expression) => expression;
    }
}
