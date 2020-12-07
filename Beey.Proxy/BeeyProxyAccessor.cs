using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Proxy
{
    internal class BeeyProxyAccessor
    {
        private static AsyncLocal<BeeyDataHolder> _beeyDataCurrent = new AsyncLocal<BeeyDataHolder>();

        public BeeyProxy? BeeyData
        {
            get
            {
                return _beeyDataCurrent?.Value?.data;
            }
            set
            {
                var holder = _beeyDataCurrent.Value;
                if (holder != null)
                {
                    // Clear current HttpContext trapped in the AsyncLocals, as its done.
                    holder.data = null;
                }

                if (value != null)
                {
                    // Use an object indirection to hold the HttpContext in the AsyncLocal,
                    // so it can be cleared in all ExecutionContexts when its cleared.
                    _beeyDataCurrent.Value = new BeeyDataHolder(value);
                }
            }
        }

        private class BeeyDataHolder
        {
            public BeeyProxy? data;

            public BeeyDataHolder(BeeyProxy value)
            {
                data = value;
            }
        }
    }
}
