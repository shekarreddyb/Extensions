using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace extensions
{
    public static class Extensions
    {

        // enumerable
        public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
        {
            foreach (var item in sequence)
                action(item);
        }

        // async await
        public static async Task<Result> TryAsync(
               this Task task,
               Action<Exception>? errorHandler = null)
        {
            try
            {
                await task;
                return Result.Ok();
            }
            catch (Exception ex)
            {
                if (errorHandler is not null) errorHandler(ex);
                return ex;
            }
        }

        public static async Task<Result<T>> TryAsync<T>(
            this Task<T> task,
            Action<Exception>? errorHandler = null) where T : class
        {
            try
            {
                return await task;
            }
            catch (Exception ex)
            {
                if (errorHandler is not null) errorHandler(ex);
                return ex;
            }
        }
        public static void Try(
            Action action,
            Action<Exception>? errorHandler = null)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                if (errorHandler is not null) errorHandler(ex);
            }
        }

        public static Result<T> Try<T>(
            Func<T> action,
            Action<Exception>? errorHandler = null) where T : class
        {
            try
            {
                return action();
            }
            catch (Exception ex)
            {
                if (errorHandler is not null) errorHandler(ex);
                return ex;
            }
        }
        public static async Task<IEnumerable<T>> WhenAllAsync<T>(this IEnumerable<Task<T>> tasks)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            return await Task
                .WhenAll(tasks)
                .ConfigureAwait(false);
        }

        public static Task WhenAllAsync(this IEnumerable<Task> tasks)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            return Task
                .WhenAll(tasks);
        }

        public static async Task<IEnumerable<T>> WhenAllSequentialAsync<T>(this IEnumerable<Task<T>> tasks)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            var results = new List<T>();
            foreach (var task in tasks)
                results.Add(await task.ConfigureAwait(false));
            return results;
        }

        public static async Task WhenAllSequentialAsync(this IEnumerable<Task> tasks)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            foreach (var task in tasks)
                await task.ConfigureAwait(false);
        }

        public static async Task<IEnumerable<T>> WhenAllParallelAsync<T>(
                this IEnumerable<Task<T>> tasks,
                int degree)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            var results = new List<T>();
            foreach (var chunk in tasks.Chunk(degree))
            {
                var chunkResults = await Task.WhenAll(chunk).ConfigureAwait(false);
                results.AddRange(chunkResults);
            }
            return results;
        }

        public static async Task WhenAllParallelAsync(
            this IEnumerable<Task> tasks,
            int degree)
        {
            if (tasks is null)
                throw new ArgumentNullException(nameof(tasks));

            foreach (var chunk in tasks.Chunk(degree))
                await Task.WhenAll(chunk).ConfigureAwait(false);
        }

        public static async Task<TOut> MapAsync<TIn, TOut>(
            this Task<TIn> task,
            Func<TIn, Task<TOut>> mapAsync)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (mapAsync is null)
                throw new ArgumentNullException(nameof(mapAsync));

            return await mapAsync(await task);
        }

        public static async Task<TOut> MapAsync<TIn, TOut>(
            this Task<TIn> task,
            Func<TIn, TOut> map)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (map is null)
                throw new ArgumentNullException(nameof(map));

            return map(await task);
        }

        public static async Task<T> DoAsync<T>(
            this Task<T> task,
            Func<T, Task> tapAsync)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (tapAsync is null)
                throw new ArgumentNullException(nameof(tapAsync));

            var res = await task;
            await tapAsync(res);
            return res;
        }

        public static async Task<T> DoAsync<T>(
                this Task<T> task,
                Action<T> tap)
        {
            if (task is null)
                throw new ArgumentNullException(nameof(task));

            if (tap is null)
                throw new ArgumentNullException(nameof(tap));

            var res = await task;
            tap(res);
            return res;
        }
        public static string Join(this IEnumerable<string> sequence, string separator = "")
        {
            return string.Join(separator, sequence);
        }


    }
    public static class Abbreviations
    {
        public static IEnumerable<T> Arr<T>(params T[] elements) => elements;
    }

}
