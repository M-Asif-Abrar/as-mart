// Models/ViewModels/ProductDetailViewModel.cs
using AsMart.Web.Models.Entities;
using System.Collections.Generic;
using System.Linq;

namespace AsMart.Web.Models.ViewModels
{
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = null!;

        public IEnumerable<ProductCardViewModel> RelatedProducts { get; set; }
            = Enumerable.Empty<ProductCardViewModel>();

        public IEnumerable<ProductCardViewModel> OtherProducts { get; set; }
            = Enumerable.Empty<ProductCardViewModel>();
    }
}
