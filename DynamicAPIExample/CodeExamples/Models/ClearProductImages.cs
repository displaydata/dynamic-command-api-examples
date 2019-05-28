using System.Collections.Generic;

namespace DynamicCommand
{
    public class ClearObjectPage
    {
        public List<string> ObjectIds { get; set; }
        public List<int> Pages { get; set; }
    }

    public class ClearProductPagesSpec
    {
        public List<ClearObjectPage> ClearObjectPages { get; set; }
    }

    public class PageResult
    {
        public int Page { get; set; }
        public bool ClearIssued { get; set; }
    }

    public class ClearProductPagesResponse
    {
        public List<string> ObjectIds { get; set; }
        public List<PageResult> PageResults { get; set; }
    }

    public class ClearProductPagesResponseList
    {
        public List<ClearProductPagesResponse> ClearObjectPagesResponse { get; set; }

    }
}