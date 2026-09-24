namespace BarangayDocumentSystem.Models
{
    public class FeeAssessment
    {
        public decimal Amount { get; private set; }
        public string Basis { get; private set; }

        public FeeAssessment(decimal amount, string basis)
        {
            Amount = amount;
            Basis = basis;
        }
    }
}
