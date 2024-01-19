using System.ComponentModel.DataAnnotations;

namespace CartServicePOC.Model
{
    public class CartRequest
    {
        [Required]
        public DateTime EffectiveDate { get; set; }
        [Required]
        public string Name { get; set; }

        [Required]
        public PriceList PriceList { get; set; }

    }

    public class PriceList
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public Guid Id { get; set; }
    }
}
