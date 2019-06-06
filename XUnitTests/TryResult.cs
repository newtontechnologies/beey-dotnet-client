using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace XUnitTests
{
    public class TryResult
    {
        public Exception? Exception { get; private set; }
        public bool IsSuccess { get => Exception == null; }

        public TryResult() { }
        public TryResult(Exception ex)
        {
            Exception = ex;
        }

        public static implicit operator bool(TryResult tryResult)
        {
            return tryResult.IsSuccess;
        }
    }

    public class TryValueResult<T> : TryResult
    {
        // hack to get rid of nullable reference warnings
        private object? value;
        public T Value
        {
            get
            {
                return IsSuccess ? (T)value! : throw new ArgumentNullException();
            }
        }

        public TryValueResult(T result) : base()
        {
            this.value = result;
        }

        public TryValueResult(Exception ex) : base(ex)
        {
            this.value = default;
        }
    }

    public static class TaskExtensions
    {
        public static async Task<TryResult> TryAsync(this Task task)
        {
            try
            {
                await task;
                return new TryResult();
            }
            catch (Exception ex)
            {
                return new TryResult(ex);
            }
        }

        public static async Task<TryValueResult<TResult>> TryAsync<TResult>(this Task<TResult> task)
        {
            try
            {
                var result = await task;
                return new TryValueResult<TResult>(result);
            }
            catch (Exception ex)
            {
                return new TryValueResult<TResult>(ex);
            }
        }
    }
}
