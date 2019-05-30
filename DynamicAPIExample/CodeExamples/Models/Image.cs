using System.Collections.Generic;

namespace DynamicCommand
{
    public class Image
    {
        public string ImageBase64 { get; set; }
        public string ObjectID { get; set; }
        public int DisplayTypeID { get; set; }
        public int PageID { get; set; }
        public string LocationName { get; set; }
        public int ImageType { get; set; }
        public string UserDefinedBatchID { get; set; }
        public string ForceUpdate { get; set; }

    }

    public class MultiProductImage
    {
        public string ImageBase64 { get; set; }
        public List<string> ObjectIDs { get; set; }
        public int DisplayTypeID { get; set; }
        public int PageID { get; set; }
        public string LocationName { get; set; }
        public int ImageType { get; set; }
        public string UserDefinedBatchID { get; set; }
        public bool ForceUpdate { get; set; }

    }
}