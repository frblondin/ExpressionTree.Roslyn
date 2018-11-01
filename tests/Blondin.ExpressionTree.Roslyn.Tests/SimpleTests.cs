using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using AutoFixture.NUnit3;
using NUnit.Framework;

namespace Blondin.ExpressionTree.Roslyn.Tests
{
    public class SimpleTests
    {
        [Test]
        [TestCaseSource(typeof(SimpleTests), nameof(TestCases))]
        public void ConstantAddition(ExpressionSyntaxVisitor sut, Expression expression, string expected)
        {
            // Act
            var result = sut.Visit(expression);

            // Assert
            Assert.That(result.ToString(), Is.EqualTo(expected));
        }

        public static IEnumerable TestCases => new[]
        {
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((int i) => 1 + i).Body,
                "1+i"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => "a" + s).Body,
                @"""a""+s"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => s.Split('c', StringSplitOptions.RemoveEmptyEntries)).Body,
                @"s.Split('c',StringSplitOptions.RemoveEmptyEntries)"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                CreateExpression((string s) => string.Intern(s)).Body,
                @"string.Intern(s)"),
            new TestCaseData(
                new ExpressionSyntaxVisitor(),
                Expression.Default(typeof(KeyValuePair<string, string>)),
                @"default(KeyValuePair<string,string>)")
        };

        static Expression<Func<T, TResult>> CreateExpression<T, TResult>(Expression<Func<T, TResult>> expression) => expression;
    }
}
