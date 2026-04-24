namespace Qrmenue.DTO
{
    public class InformStateDTO
    {

        public string result { get; set; }
        public int PrintRequestTime { get; set; }
        public int StateRequestTime { get; set; }
        public bool Close { get; set; }
        public bool Restart { get; set; }
        public bool ErrorLog { get; set; }
    }
}
