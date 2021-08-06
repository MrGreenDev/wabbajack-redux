using System;
using System.Collections.Generic;

namespace Wabbajack.Common
{
    public static class IEnumerableExtensions
    {
        public static void Do<T>(this IEnumerable<T> coll, Action<T> f)
        {
            foreach (var i in coll) f(i);
        }

    }
}