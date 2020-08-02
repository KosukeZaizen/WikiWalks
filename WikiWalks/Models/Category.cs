using System;
using System.Collections.Generic;

namespace RelatedPages.Models
{
    public class Category
    {
        public string category { get; set; }
        public IEnumerable<int> wordIds { get; set; }
        public int cnt { get; set; }
    }
}