using System;
using System.Collections.Generic;
using System.Text;

namespace SpeakerDbUpdater
{
    class CustomEqualityComparer<T> : IEqualityComparer<T>
    {
        protected Func<T, T, bool> Compare { get; }

        public CustomEqualityComparer(Func<T, T, bool> equalityComparer)
        {
            Compare = equalityComparer;
        }

        public bool Equals(T x, T y)
        {
            return Compare(x, y);
        }

        public int GetHashCode(T obj)
        {
            return -1;
        }

        public static explicit operator CustomEqualityComparer<T>(Func<T, T, bool> equalityComparer)
        {
            return new CustomEqualityComparer<T>(equalityComparer);
        }
    }
}
