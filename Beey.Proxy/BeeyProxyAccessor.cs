using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Proxy;

internal partial class BeeyProxyAccessor
{
    private static AsyncLocal<BeeyProxyHolder> _beeyDataCurrent = new AsyncLocal<BeeyProxyHolder>();

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
                _beeyDataCurrent.Value = new BeeyProxyHolder(value);
            }
        }
    }

    public BeeyProxyHolder BeeyDataHolder
    {
        get
        {
            return _beeyDataCurrent?.Value ?? BeeyProxyHolder.Empty;
        }
    }
}
