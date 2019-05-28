using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeeyUI
{
    public class TryResult
    {
        public Exception? Ex { get; private set; }
        public bool Success { get => Ex == null; }

        public TryResult(Exception? ex)
        {
            Ex = ex;
        }

        public static implicit operator bool(TryResult tryResult)
        {
            return tryResult.Success;
        }
    }

    public class TryReferenceResult<T> : TryResult where T : class
    {
        public T? Result { get; private set; }

        public TryReferenceResult(Exception? ex, T? result) : base(ex)
        {
            Result = result;
        }

        public static implicit operator bool(TryReferenceResult<T> tryResult)
        {
            return tryResult.Success;
        }
    }

    public class TryValueResult<T> : TryResult where T : struct
    {
        public T? Result { get; private set; }

        public TryValueResult(Exception? ex, T? result) : base(ex)
        {
            Result = result;
        }

        public static implicit operator bool(TryValueResult<T> tryResult)
        {
            return tryResult.Success;
        }
    }

    public static class TaskExtensions
    {
        public static async Task<TryResult> TryAsync(this Task task)
        {
            try
            {
                await task;
                return new TryResult(null);
            }
            catch (Exception ex)
            {
                return new TryResult(ex);
            }
        }

        public static async Task<TryReferenceResult<TResult>> TryRefAsync<TResult>(this Task<TResult> task)
            where TResult: class
        {
            try
            {
                var result = await task;
                return new TryReferenceResult<TResult>(null, result);
            }
            catch (Exception ex)
            {
                return new TryReferenceResult<TResult>(ex, null);
            }
        }

        public static async Task<TryValueResult<TResult>> TryValAsync<TResult>(this Task<TResult> task)
            where TResult : struct
        {
            try
            {
                var result = await task;
                return new TryValueResult<TResult>(null, result);
            }
            catch (Exception ex)
            {
                return new TryValueResult<TResult>(ex, null);
            }
        }
    }
}
