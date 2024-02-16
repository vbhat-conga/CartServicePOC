namespace CartServicePOC.Exceptions
{
    public class PriceListIdNotFoundException : Exception
    {
        public PriceListIdNotFoundException() : base()
        {

        }

        public PriceListIdNotFoundException(string message) : base(message)
        {

        }

        public PriceListIdNotFoundException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
