using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.Emit;

namespace Blondin.ExpressionTree.Roslyn.Tests.Tools
{
    internal static class CompilationHelper
    {
        private static readonly MetadataReference _coreLib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);

        internal static MemoryStream CompileAssembly(string name, ClassDeclarationSyntax @class, IEnumerable<UsingDirectiveSyntax> usings, params MetadataReference[] references)
        {
            var unit = CompilationUnit()
                .WithUsings(
                    List(usings))
                .WithMembers(
                    SingletonList<MemberDeclarationSyntax>(
                        NamespaceDeclaration(IdentifierName(name))
                        .WithMembers(
                            SingletonList<MemberDeclarationSyntax>(@class))))
                .NormalizeWhitespace();
            return CompileAssembly(name, unit, references);
        }

        public static MemoryStream CompileAssembly(string name, CompilationUnitSyntax unit, params MetadataReference[] references)
        {
            var compilation = CSharpCompilation.Create(name)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddReferences(references.Prepend(_coreLib))
                .AddSyntaxTrees(unit.SyntaxTree);
            return CompileAssembly(compilation);
        }

        private static MemoryStream CompileAssembly(CSharpCompilation compilation)
        {
            var stream = new MemoryStream();
            var result = compilation.Emit(stream);
            ThrowIfFailed(result);
            stream.Seek(0L, SeekOrigin.Begin);
            return stream;
        }

        private static void ThrowIfFailed(EmitResult result)
        {
            if (!result.Success)
            {
                throw new AggregateException(
                    $"Compilation failed with {result.Diagnostics.Length} errors.",
                    result.Diagnostics.Select(d => new InvalidOperationException(d.ToString())));
            }
        }
    }
}
