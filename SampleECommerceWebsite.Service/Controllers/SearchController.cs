using Microsoft.AspNetCore.Mvc;
using SampleECommerceWebsite.DAL.EF.Repos.Interfaces;
using SampleECommerceWebsite.Models.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SampleECommerceWebsite.Service.Controllers
{
    [Route("api/[controller]")]
    public class SearchController : Controller
    {
        private IProductRepo Repo { get; set; }
        public SearchController(IProductRepo repo)
        {
            Repo = repo;
        }

        [HttpGet("{searchString}", Name = "SearchProducts")]
        public IEnumerable<ProductAndCategoryBase> Search(string searchString) => Repo.Search(searchString);
        //pursuade%20anyone
    }
}
