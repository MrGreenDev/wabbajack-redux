using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public static class AsyncParallelExtensions
    {
        public static async IAsyncEnumerable<TOut> PMap<TIn, TOut>(this IEnumerable<TIn> coll, IRateLimiter limiter, Func<TIn, ValueTask<TOut>> mapFn)
        {
            List<Task<TOut>> outs = coll.Select(itm => Task.Run(async () =>
                {
                    using var _ = await limiter.Checkout();
                    return await mapFn(itm);
                }))
                .ToList();
            
            foreach (var result in outs)
            {
                yield return await result;
            }
        }
        
        public static async Task PDo<TIn>(this IEnumerable<TIn> coll, IRateLimiter limiter, Func<TIn, ValueTask> mapFn)
        {
            List<Task> outs = coll.Select(itm => Task.Run(async () =>
                {
                    using var _ = await limiter.Checkout();
                    await mapFn(itm);
                }))
                .ToList();
            
            await Task.WhenAll(outs);
        }

        public static async Task<IList<T>> ToList<T>(this IAsyncEnumerable<T> coll)
        {
            List<T> lst = new();
            await foreach (var itm in coll)
            {
                lst.Add(itm);
            }
            return lst;
        }
        
        public static async Task<IDictionary<TK, T>> ToDictionary<T, TK>(this IAsyncEnumerable<T> coll, Func<T, TK> kSelector)
            where TK: notnull
        {
            Dictionary<TK, T> dict = new();
            await foreach (var itm in coll)
            {
                dict.Add(kSelector(itm), itm);
            }
            return dict;
        }
        
        public static async Task<IDictionary<TK, TV>> ToDictionary<T, TK, TV>(this IAsyncEnumerable<T> coll, Func<T, TK> kSelector, Func<T, TV> vSelector)
            where TK: notnull
        {
            Dictionary<TK, TV> dict = new();
            await foreach (var itm in coll)
            {
                dict.Add(kSelector(itm), vSelector(itm));
            }
            return dict;
        }

        public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> coll, Predicate<T> p)
        {
            await foreach (var itm in coll)
            {
                if (p(itm)) yield return itm;
            }
        }
    }
}