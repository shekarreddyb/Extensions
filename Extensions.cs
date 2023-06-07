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
        
        //usage: SendEmailAsync().FireAndForget(errorHandler => Console.WriteLine(errorHandler.Message));
        public static void FireAndForget(this Task task,  Action<Exception> errorHandler = null)
        {
             task.ContinueWith(t =>
              {
                if (t.IsFaulted && errorHandler != null)
                    errorHandler(t.Exception);
              }, TaskContinuationOptions.OnlyOnFaulted);
         }
        
        // usage: var result = await (() => GetResultAsync()).Retry(3, TimeSpan.FromSeconds(1));
        public static async Task<TResult> Retry<TResult>(this Func<Task<TResult>> taskFactory, int maxRetries, TimeSpan delay)
        {
            for (int i = 0; i < maxRetries; i++)
            {
                try
                {
                    return await taskFactory().ConfigureAwait(false);
                }
                catch
                {
                    if (i == maxRetries - 1)
                        throw;
                    await Task.Delay(delay).ConfigureAwait(false);
                }
            }

            return default(TResult); // Should not be reached
        }
       // usage: await GetResultAsync().OnFailure(ex => Console.WriteLine(ex.Message));
        public static async Task OnFailure(this Task task, Action<Exception> onFailure)
        {
            try
            {
                await task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                onFailure(ex);
            }
        }
        
        // usage:  await GetResultAsync().WithTimeout(TimeSpan.FromSeconds(1));
        // .net6 has WaitAsync (TimeSpan timeout); https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.task.waitasync?view=net-7.0#system-threading-tasks-task-waitasync(system-timespan)
        public static async Task WithTimeout(this Task task, TimeSpan timeout)
        {
            var delayTask = Task.Delay(timeout);
            var completedTask = await Task.WhenAny(task, delayTask).ConfigureAwait(false);
            if (completedTask == delayTask)
                throw new TimeoutException();

            await task;
        }
        
        // usage:  var result = await GetResultAsync().Fallback("fallback");
        public static async Task<TResult> Fallback<TResult>(this Task<TResult> task, TResult fallbackValue)
        {
            try
            {
                return await task.ConfigureAwait(false);
            }
            catch
            {
                return fallbackValue;
            }
        }
        
    }
    public static class Abbreviations
    {
        public static IEnumerable<T> Arr<T>(params T[] elements) => elements;
    }

}
