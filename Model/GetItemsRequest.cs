using Microsoft.Build.Framework;

namespace CartServicePOC.Model
{
    public class GetItemsRequest
    {
        [Required]
        public IEnumerable<Guid> Ids { get; set; }

        public IEnumerable<string>? Fields { get; set; }
    }
}
