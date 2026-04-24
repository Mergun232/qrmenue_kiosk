using System.Collections.Generic;

namespace Qrmenue.DTO
{
    public class FicheDTO
    {
        public List<Fiche> Printer { get; set; }
    }
    public class Fiche
    {
        public string PrinterID { get; set; }
        public string PrinterName { get; set; }
        public string IP { get; set; }
        public string Port { get; set; }
        private string myVar;

        public string PrinterData
        {
            get { return myVar.Replace("\\n", "").Replace("\\r", "").Replace("\\", ""); }
            set { myVar = value; }
        }
        public int pageHeight { get; set; }

        public int pageWidth { get; set; }

    }
}
