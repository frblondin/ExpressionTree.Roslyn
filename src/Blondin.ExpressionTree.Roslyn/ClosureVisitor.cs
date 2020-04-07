using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Blondin.ExpressionTree.Roslyn
{
    internal class ClosureVisitor : ExpressionVisitor
    {
        private List<ParameterExpression> _currentClosureVariables = new List<ParameterExpression>();

        public IList<ParameterExpression> CapturedVariables { get; } = new List<ParameterExpression>();

        internal static ClosureVisitor VisitExpression(Expression expression)
        {
            var result = new ClosureVisitor();
            result.Visit(expression);
            return result;
        }

        protected override Expression VisitLambda<T>(Expression<T> node)
        {
            var previousVariables = _currentClosureVariables;

            _currentClosureVariables = new List<ParameterExpression>(_currentClosureVariables.Concat(node.Parameters));
            var result = base.VisitLambda(node);
            _currentClosureVariables = previousVariables;

            return result;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (!_currentClosureVariables.Contains(node))
            {
                CapturedVariables.Add(node);
            }
            return base.VisitParameter(node);
        }

        protected override Expression VisitBlock(BlockExpression node)
        {
            var previousVariables = _currentClosureVariables;

            _currentClosureVariables = new List<ParameterExpression>(_currentClosureVariables.Concat(node.Variables));
            var result = base.VisitBlock(node);
            _currentClosureVariables = previousVariables;

            return result;
        }
    }
}
