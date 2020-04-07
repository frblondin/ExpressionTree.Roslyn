using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Blondin.ExpressionTree.Roslyn.Mono
{
    public enum CustomExpressionType
    {
        DoWhileExpression,
        ForEachExpression,
        ForExpression,
        UsingExpression,
        WhileExpression,
    }

    public abstract partial class CustomExpression : Expression
    {

        public abstract CustomExpressionType CustomNodeType { get; }

        public override ExpressionType NodeType
        {
            get { return ExpressionType.Extension; }
        }

        public override bool CanReduce
        {
            get { return true; }
        }
    }
}
