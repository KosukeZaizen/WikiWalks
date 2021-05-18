using System.Collections.Generic;

namespace RelatedPages.Models
{
    public class Page
    {
        public int wordId { get; set; }
        public string word { get; set; }
        public string snippet { get; set; }
        public int referenceCount { get; set; }
        public bool? isAboutJapan {
            get; set;
        }
    }
}