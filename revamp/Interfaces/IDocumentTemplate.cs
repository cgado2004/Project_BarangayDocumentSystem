using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Interfaces
{
    public interface IDocumentTemplate
    {
        DocumentType DocumentType { get; }
        string Title { get; }
        string BuildBody(DocumentRequest request);
    }
}
