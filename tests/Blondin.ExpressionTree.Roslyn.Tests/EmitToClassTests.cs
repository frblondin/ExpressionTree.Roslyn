using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.IO;
using System.Reflection;
using Blondin.ExpressionTree.Roslyn.Tests.Tools;
using System.Collections.Immutable;
using System.Runtime.Loader;

namespace Blondin.ExpressionTree.Roslyn.Tests
{
    public class EmitToClassTests
    {
        [Test]
        public void LambdaToMethod()
        {
            // Act
            var stream = CreateAddOneMethod();

            // Assert
            var assembly = Assembly.Load(stream.ToArray());
            var addOne = assembly.LookupDelegate<Func<int, int>>($"{nameof(LambdaToMethod)}Assembly.{nameof(LambdaToMethod)}Class", "AddOne");
            Assert.That(addOne(3), Is.EqualTo(4));
        }

        private MemoryStream CreateAddOneMethod()
        {
            // Arrange
            var i = Expression.Parameter(typeof(int), "i");
            var j = Expression.Parameter(typeof(int), "j");
            var expression = Expression.Lambda<Func<int, int>>(
                Expression.Block(
                    typeof(int),
                    new[] { j },
                    Expression.Assign(j, Expression.Constant(1)),
                    Expression.Add(i, j)),
                "AddOne",
                Enumerable.Repeat(i, 1));
            var type = ClassDeclaration($"{nameof(LambdaToMethod)}Class")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)));

            // Act
            var sut = new ExpressionSyntaxVisitor(type);
            var visited = sut.Visit(expression).NormalizeWhitespace();

            // Assert
            Assert.That(visited.ToString(), Is.EqualTo(
@"public static int AddOne(int i)
{
    int j;
    j = 1;
    return i + j;
}"));
            var stream = CompilationHelper.CompileAssembly($"{nameof(LambdaToMethod)}Assembly", sut.Class, sut.Namespaces);
            stream.Seek(0L, SeekOrigin.Begin);
            return stream;
        }

        [Test]
        public void LambdaContainingClosureLambdaToMethod()
        {
            // Arrange
            var i = Expression.Parameter(typeof(int), "i");
            var j = Expression.Parameter(typeof(int), "j");
            var sumUp = Expression.Lambda<Func<int>>(
                Expression.Add(i, j),
                "AddIAndJ",
                Enumerable.Empty<ParameterExpression>());
            var expression = Expression.Lambda<Func<int, int>>(
                Expression.Block(
                    typeof(int),
                    new[] { j },
                    Expression.Assign(j, Expression.Constant(1)),
                    Expression.Call(
                        sumUp,
                        "Invoke",
                        null)),
                "AddOne",
                Enumerable.Repeat(i, 1));
            var type = ClassDeclaration($"{nameof(LambdaContainingClosureLambdaToMethod)}Class")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)));

            // Act
            var sut = new ExpressionSyntaxVisitor(type);
            var visited = sut.Visit(expression).NormalizeWhitespace();

            // Assert
            Assert.That(visited.ToString(), Is.EqualTo(
@"public static int AddOne(int i)
{
    int j;
    j = 1;
    return new Func<int>(() => i + j).Invoke();
}"));
            var stream = CompilationHelper.CompileAssembly($"{nameof(LambdaContainingClosureLambdaToMethod)}Assembly", sut.Class, sut.Namespaces);
            var assembly = Assembly.Load(stream.ToArray());
            var addOne = assembly.LookupDelegate<Func<int, int>>($"{nameof(LambdaContainingClosureLambdaToMethod)}Assembly.{nameof(LambdaContainingClosureLambdaToMethod)}Class", "AddOne");
            Assert.That(addOne(3), Is.EqualTo(4));
        }

        [Test]
        public void LambdaCallingAssemblyMember()
        {
            // Arrange
            var previouslyCompiledContent = CreateAddOneMethod();
            var previouslyCompiledAssembly = AssemblyLoadContext.Default.LoadFromStream(previouslyCompiledContent);
            var addOneMethod = previouslyCompiledAssembly.LookupMethod($"{nameof(LambdaToMethod)}Assembly.{nameof(LambdaToMethod)}Class", "AddOne");
            var i = Expression.Parameter(typeof(int), "i");
            var expression = Expression.Lambda<Func<int, int>>(
                Expression.Call(
                    addOneMethod,
                    i),
                "AddOne",
                new[] { i });
            var type = ClassDeclaration($"{nameof(LambdaCallingAssemblyMember)}Class")
                .WithModifiers(
                    TokenList(
                        Token(SyntaxKind.PublicKeyword)));

            // Act
            var sut = new ExpressionSyntaxVisitor(type);
            var visited = sut.Visit(expression).NormalizeWhitespace();

            // Assert
            Assert.That(visited.ToString(), Is.EqualTo(
$@"public static int AddOne(int i)
{{
    return {nameof(LambdaToMethod)}Class.AddOne(i);
}}"));
            var stream = CompilationHelper.CompileAssembly($"{nameof(LambdaCallingAssemblyMember)}Assembly", sut.Class, sut.Namespaces, MetadataReference.CreateFromImage(previouslyCompiledContent.ToArray().ToImmutableArray()));
            var assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            var addOne = assembly.LookupDelegate<Func<int, int>>($"{nameof(LambdaCallingAssemblyMember)}Assembly.{nameof(LambdaCallingAssemblyMember)}Class", "AddOne");
            Assert.That(addOne(3), Is.EqualTo(4));
        }

        public int AddOne(int i) => i + 1;
    }
}
