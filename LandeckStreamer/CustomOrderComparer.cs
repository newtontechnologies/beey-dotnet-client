using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LandeckTranscriber
{
    class CustomOrderComparer : IComparer<string>
    {
        public string[] Order { get; }
        public CustomOrderComparer(IEnumerable<string> order, IEnumerable<string> orderOverrides)
        {
            var norder = order.Select(l => l.ToLower()).ToArray();
            var noorder = orderOverrides.Select(l => l.Trim().ToLower()).Where(l=>!string.IsNullOrWhiteSpace(l)).ToArray();

            Order = noorder.Concat(norder.Except(noorder)).ToArray();
        }

        public int Compare(string x, string y)
        {
            var idx = Array.IndexOf(Order, x);
            var idy = Array.IndexOf(Order, y);

            if (idx < 0)
                idx = int.MaxValue;

            if (idy < 0)
                idy = int.MaxValue;

            var comp = idx.CompareTo(idy);

            return (comp == 0) ? x.CompareTo(y) : comp;
        }
    }
}
