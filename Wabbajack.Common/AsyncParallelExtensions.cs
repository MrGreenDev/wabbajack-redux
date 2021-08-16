using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Wabbajack.Common
{
    public static class AsyncParallelExtensions
    {
        public static async IAsyncEnumerable<TOut> PMap<TIn, TOut>(this IEnumerable<TIn> coll, IRateLimiter limiter,
            Func<TIn, Task<TOut>> mapFn)
        {
            var tasks = coll.Select(itm => limiter.Enqueue(() => mapFn(itm))).ToList();

            CancellationTokenSource cts = new();
            limiter.Assist(cts.Token);


            foreach (var result in tasks) yield return await result;
            cts.Cancel();
        }

        public static async Task PDo<TIn>(this IEnumerable<TIn> coll, IRateLimiter limiter, Func<TIn, Task> mapFn)
        {
            var tasks = coll.Select(itm => limiter.Enqueue(() => mapFn(itm)))
                .ToArray();

            CancellationTokenSource cts = new();
            limiter.Assist(cts.Token);

            await Task.WhenAll(tasks);

            tasks.Where(t => t.IsFaulted).Do(f => throw f.Exception!);

            cts.Cancel();
        }

        public static async Task<List<T>> ToList<T>(this IAsyncEnumerable<T> coll)
        {
            List<T> lst = new();
            await foreach (var itm in coll) lst.Add(itm);
            return lst;
        }
        
        public static async Task<T[]> ToArray<T>(this IAsyncEnumerable<T> coll)
        {
            List<T> lst = new();
            await foreach (var itm in coll) lst.Add(itm);
            return lst.ToArray();
        }
        
        public static async Task<IReadOnlyCollection<T>> ToReadOnlyCollection<T>(this IAsyncEnumerable<T> coll)
        {
            List<T> lst = new();
            await foreach (var itm in coll) lst.Add(itm);
            return lst;
        }
        
        public static async Task<HashSet<T>> ToHashSet<T>(this IAsyncEnumerable<T> coll, Predicate<T>? filter = default)
        {
            HashSet<T> lst = new();
            if (filter == default)
            {
                await foreach (var itm in coll) lst.Add(itm);
            }
            else
            {
                await foreach (var itm in coll.Where(filter)) lst.Add(itm);
            }

            return lst;
        }

        public static async Task Do<T>(this IAsyncEnumerable<T> coll, Func<T, Task> fn)
        {
            await foreach (var itm in coll) await fn(itm);
        }

        public static async Task Do<T>(this IAsyncEnumerable<T> coll, Action<T> fn)
        {
            await foreach (var itm in coll) fn(itm);
        }

        public static async Task<IDictionary<TK, T>> ToDictionary<T, TK>(this IAsyncEnumerable<T> coll,
            Func<T, TK> kSelector)
            where TK : notnull
        {
            Dictionary<TK, T> dict = new();
            await foreach (var itm in coll) dict.Add(kSelector(itm), itm);
            return dict;
        }

        public static async Task<IDictionary<TK, TV>> ToDictionary<T, TK, TV>(this IAsyncEnumerable<T> coll,
            Func<T, TK> kSelector, Func<T, TV> vSelector)
            where TK : notnull
        {
            Dictionary<TK, TV> dict = new();
            await foreach (var itm in coll) dict.Add(kSelector(itm), vSelector(itm));
            return dict;
        }

        public static async IAsyncEnumerable<T> Where<T>(this IAsyncEnumerable<T> coll, Predicate<T> p)
        {
            await foreach (var itm in coll)
                if (p(itm))
                    yield return itm;
        }
        
        public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IEnumerable<TIn> coll, Func<TIn, ValueTask<TOut>> fn)
        {
            foreach (var itm in coll)
                yield return await fn(itm);
        }
        
        public static async IAsyncEnumerable<TOut> SelectMany<TIn, TOut>(this IEnumerable<TIn> coll, Func<TIn, ValueTask<IEnumerable<TOut>>> fn)
        {
            foreach (var itm in coll)
                foreach (var inner in await fn(itm)) 
                    yield return inner;
        }
        
        public static async IAsyncEnumerable<TOut> Select<TIn, TOut>(this IAsyncEnumerable<TIn> coll, Func<TIn, ValueTask<TOut>> fn)
        {
            await foreach (var itm in coll)
                yield return await fn(itm);
        }
        
        public static async IAsyncEnumerable<TOut> SelectMany<TIn, TOut>(this IAsyncEnumerable<TIn> coll, Func<TIn, IEnumerable<TOut>> fn)
        {
            await foreach (var itm in coll)
                foreach (var inner in fn(itm))
                    yield return inner;
        }
    }
}