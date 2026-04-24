namespace PrintOptions
{
    public struct Printer
    {
        public string name { get; set; }
        public string status { get; set; }
        public bool isDefault { get; set; }
        public bool isNetworkPrinter { get; set; }
        public string Path { get; set; }
        public string NamespacePath { get; set; }
        public string RelativePath { get; set; }
        public string Server { get; set; }

        public Printer(string name, string status, bool isDefault, bool isNetworkPrinter,
            string Path,
            string NamespacePath,
            string RelativePath,
            string Server)
        {
            this.name = name;
            this.status = status;
            this.isDefault = isDefault;
            this.isNetworkPrinter = isNetworkPrinter;

            this.Path = Path;
            this.NamespacePath = NamespacePath;
            this.RelativePath = RelativePath;
            this.Server = Server;

        }
    }

}
