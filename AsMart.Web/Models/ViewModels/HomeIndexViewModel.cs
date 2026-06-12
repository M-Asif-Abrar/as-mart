// Models/ViewModels/HomeIndexViewModel.cs
using System.Collections.Generic;
using AsMart.Web.Models.Entities;

namespace AsMart.Web.Models.ViewModels
{
    public class HomeIndexViewModel
    {
        public List<Product> Featured { get; set; } = new();
        public List<Product> Deals { get; set; } = new();
    }
}
