using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Blondin.ExpressionTree.Roslyn.Tests.Tools
{
    internal static class ReflectionHelper
    {
        internal static MethodInfo LookupMethod(this Assembly assembly, string typeName, string methodName)
        {
            var type = assembly.GetType(typeName);
            return type.GetMethod(methodName);
        }

        internal static TDelegate LookupDelegate<TDelegate>(this Assembly assembly, string typeName, string methodName)
            where TDelegate : Delegate
        {
            var method = assembly.LookupMethod(typeName, methodName);
            return (TDelegate)method.CreateDelegate(typeof(TDelegate));
        }
    }
}
