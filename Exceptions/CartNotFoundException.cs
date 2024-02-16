namespace CartServicePOC.Exceptions
{
    public class CartNotFoundException : Exception
    {
        public CartNotFoundException() : base()
        {

        }

        public CartNotFoundException(string message) : base(message)
        {

        }

        public CartNotFoundException(string message, Exception ex) : base(message, ex)
        {

        }
    }
}
