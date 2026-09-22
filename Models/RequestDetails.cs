namespace BarangayDocumentSystem.Models
{
    public class RequestDetails
    {
        public int ResidentId { get; set; }
        public DocumentType DocumentType { get; set; }
        public string Purpose { get; set; } = "";
        public string BusinessName { get; set; } = "";
        public string BusinessAddress { get; set; } = "";
        public string BusinessNature { get; set; } = "";
    }
}
