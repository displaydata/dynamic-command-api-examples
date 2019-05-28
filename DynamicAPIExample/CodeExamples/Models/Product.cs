using System;
using System.Collections.Generic;

namespace DynamicCommand
{
    public class Product
    {
        public string ObjectID { get; set; }
        public int Sequence { get; set; }
        public string ObjectName { get; set; }
        public string ObjectDescription { get; set; }
        public List<string> SearchableValues { get; set; }
        public int ClientID { get; set; }
        public Guid LastModifiedBy { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public int NoStores { get; set; }
        public int NoAssignedDisplayds { get; set; }
        public bool HasOverride { get; set; }
        public int NoOverrides { get; set; }
        public Uri ApiLocation { get; set; }
    }

    public class ProductSearchResult
    {
        public List<Product> Objects { get; set; }
    }
}