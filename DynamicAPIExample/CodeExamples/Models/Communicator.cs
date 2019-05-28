namespace DynamicCommand
{
    public class NetworkConfig
    {
        public int Method { get; set; }
        public string IPAddress { get; set; }
        public int PrefixLength { get; set; }
        public string DefaultGateway { get; set; }
    }

    public class CommunicatorFirmware
    {
        public string Version { get; set; }
    }

    public class Communicator
    {
        public string SerialNumber { get; set; }
        public string Mode { get; set; }
        public string MACAddress { get; set; }
        public string Nameservers { get; set; }
        public string Domains { get; set; }
        public string IPAddress { get; set; }
        public NetworkConfig NetworkConfig { get; set; }
        public string Hostname { get; set; }
        public string NetworkID { get; set; }
        public int Status { get; set; }
        public string Comments { get; set; }
        public string Description { get; set; }
        public string LocationName { get; set; }
        public int ClientID { get; set; }
        public bool Enabled { get; set; }
        public string LastRSSIDate { get; set; }
        public string BackupObjects { get; set; }
        public int Channel { get; set; }
        public string FirmwareVersion { get; set; }
        public bool KeepCommunicatorNetworkId { get; set; }
    }
}
