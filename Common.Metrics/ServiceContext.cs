namespace AcmeCli
{
    public class ServiceContext
    {
        public string Role { get; set; }
        public string Namespace { get; set; }
        public string Version { get; set; }
        public string[] Tags { get; set; }
    }
}