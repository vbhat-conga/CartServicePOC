namespace CartServicePOC.DataModel
{
    public class CartData
    {
        public Guid CartId { get; set; }
        public string Name { get; set; }
        public Guid PriceListId { get; set; }
        public CartStatus Status { get; set; }

        public IEnumerable<CartItemData> CartItems { get; set; }

        public double Price { get; set; } = 0.0;
        //public virtual PriceListData PriceList { get; set; }
    }

    public enum CartStatus
    {
        Unknown = 0,
        Created = 1,
        Conigured = 2,
        Priced = 3
    }

    public class CartStatusType
    {
        public int Id { get; set; }
        public string Status {get;set;}
    }
}
