namespace DynamicCommand
{
    public class GetDisplaysResponse
    {
        public string SerialNumber { get; set; }
        public int DisplayTypeID { get; set; }
        public string DisplayTypeName { get; set; }
        public int LocationID { get; set; }
        public string LocationName { get; set; }
        public int ClientID { get; set; }
        public int Pages { get; set; }
    }
}
