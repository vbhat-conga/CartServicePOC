using Newtonsoft.Json;
using StackExchange.Redis;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using JsonIgnoreAttribute = System.Text.Json.Serialization.JsonIgnoreAttribute;

namespace CartServicePOC.Model
{
    public class CartItemRequest
    {
        [Required]
        public Guid ItemId { get; set; }
        [Required]
        public bool IsPrimaryLine { get; set; }
        [Required]
        public LineType LineType { get; set; } = LineType.ProductService;
        [Required]
        public int Quantity { get; set; } = 1;

        [Required]    
        public string ExternalId { get; set; }
        [Required]
        public int PrimaryTaxLineNumber { get; set; }

        [Required]
        public Product Product { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
        public Guid CartId { get; set; }

    }

    public enum LineType
    {
        None = 0,
        ProductService =1
    }


    public class Product
    {

        [Required]
        public Guid Id { get; set; }
    }
}
