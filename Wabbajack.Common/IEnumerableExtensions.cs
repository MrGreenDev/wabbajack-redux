using System;
using System.Collections.Generic;
using System.Linq;

namespace Wabbajack.Common
{
    public static class IEnumerableExtensions
    {
        public static void Do<T>(this IEnumerable<T> coll, Action<T> f)
        {
            foreach (var i in coll) f(i);
        }


        public static IEnumerable<TOut> TryKeep<TIn, TOut>(this IEnumerable<TIn> coll, Func<TIn, (bool, TOut)> fn)
        {
            return coll.Select(fn).Where(p => p.Item1).Select(p => p.Item2);
        }
    }
}