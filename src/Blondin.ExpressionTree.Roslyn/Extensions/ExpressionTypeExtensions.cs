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
                case ExpressionType.Assign: return SyntaxKind.SimpleAssignmentExpression;
                case ExpressionType.AddAssign:
                case ExpressionType.AddAssignChecked: return SyntaxKind.AddAssignmentExpression;
                case ExpressionType.AndAssign: return SyntaxKind.AndAssignmentExpression;
                case ExpressionType.DivideAssign: return SyntaxKind.DivideAssignmentExpression;
                case ExpressionType.ExclusiveOrAssign: return SyntaxKind.ExclusiveOrAssignmentExpression;
                case ExpressionType.LeftShiftAssign: return SyntaxKind.LeftShiftAssignmentExpression;
                case ExpressionType.ModuloAssign: return SyntaxKind.ModuloAssignmentExpression;
                case ExpressionType.MultiplyAssign:
                case ExpressionType.MultiplyAssignChecked: return SyntaxKind.MultiplyAssignmentExpression;
                case ExpressionType.OrAssign: return SyntaxKind.OrAssignmentExpression;
                case ExpressionType.PostDecrementAssign: return SyntaxKind.PostDecrementExpression;
                case ExpressionType.PostIncrementAssign: return SyntaxKind.PostIncrementExpression;
                case ExpressionType.PowerAssign: return SyntaxKind.ExclusiveOrAssignmentExpression;
                case ExpressionType.RightShiftAssign: return SyntaxKind.RightShiftAssignmentExpression;
                case ExpressionType.SubtractAssign:
                case ExpressionType.SubtractAssignChecked: return SyntaxKind.SubtractAssignmentExpression;

                case ExpressionType.PreDecrementAssign: return SyntaxKind.PreDecrementExpression;
                case ExpressionType.PreIncrementAssign: return SyntaxKind.PreIncrementExpression;

                case ExpressionType.LessThan: return SyntaxKind.LessThanExpression;
                case ExpressionType.LessThanOrEqual: return SyntaxKind.LessThanOrEqualExpression;
                case ExpressionType.GreaterThan: return SyntaxKind.GreaterThanExpression;
                case ExpressionType.GreaterThanOrEqual: return SyntaxKind.GreaterThanOrEqualExpression;

                case ExpressionType.Increment: return SyntaxKind.PostIncrementExpression;
                case ExpressionType.Decrement: return SyntaxKind.PostDecrementExpression;
                case ExpressionType.UnaryPlus: return SyntaxKind.UnaryPlusExpression;
                case ExpressionType.Negate: return SyntaxKind.UnaryMinusExpression;
                case ExpressionType.OnesComplement: return SyntaxKind.BitwiseNotExpression;

                default: throw new NotSupportedException($"Expression type {type} is not supported.");
            }
        }
    }
}
