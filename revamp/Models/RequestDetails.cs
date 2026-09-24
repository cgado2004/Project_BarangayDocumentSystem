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

        /// <summary>Local or abroad, for the Barangay Clearance only. The
        /// Charter prices the clearance PHP 100 locally and PHP 200 abroad;
        /// every other document ignores this field.</summary>
        public ClearanceScope Scope { get; set; } = ClearanceScope.Local;
    }
}
