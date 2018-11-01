using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp;

namespace System.Linq.Expressions
{
    internal static class ExpressionTypeExtensions
    {
        internal static SyntaxKind ToSyntaxKind(this ExpressionType type)
        {
            switch (type)
            {
                case ExpressionType.Add: return SyntaxKind.AddExpression;
                default: throw new NotSupportedException($"Expression type {type} is not supported.");
            }
        }
    }
}
