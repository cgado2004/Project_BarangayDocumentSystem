namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// The Citizen's Charter prices ONE document two ways: a Barangay Clearance
    /// costs PHP 100 for local employment and PHP 200 for work abroad. The scope
    /// is chosen on the request form and travels with the request everywhere:
    /// through the fee assessment, into the database, and back out on reopen.
    /// </summary>
    public enum ClearanceScope
    {
        Local,
        Abroad
    }
}
