namespace Beey.Proxy
{
    public class BeeyProxyHolder
    {
        public BeeyProxy? data;

        internal BeeyProxyHolder(BeeyProxy? value)
        {
            data = value;
        }

        public static BeeyProxyHolder Empty { get; } = new BeeyProxyHolder(null);
    }
}
