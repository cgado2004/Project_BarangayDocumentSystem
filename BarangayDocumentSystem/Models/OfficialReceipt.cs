// ---------------------------------------------------------------------------
//  OfficialReceipt.cs - the government receipt (OR) and the booklet it came
//  from. This is the part the auditor asks about.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// One government official receipt.
    ///
    /// Two numbers matter here and I keep both, because the barangay treasurer
    /// asked for them:
    ///
    ///   * the OR number - the number printed on the receipt I hand over;
    ///   * the control number - the number in the booklet's series control,
    ///     which is how a hundred receipts are tracked as one accountable
    ///     batch.
    ///
    /// A receipt is never edited and never deleted. If a clerk writes the
    /// wrong figure, the receipt is voided with a reason and a new one is
    /// issued, and both rows stay in the table. That is how a real collection
    /// register behaves.
    /// </summary>
    public class OfficialReceipt
    {
        public int ReceiptId { get; internal set; }

        /// <summary>The OR number printed on the receipt, e.g. "2026-000451".</summary>
        public string OrNumber { get; set; }

        /// <summary>The series control of the booklet this receipt came from,
        /// e.g. "A". The series is what the Commission on Audit inspects.</summary>
        public string SeriesCode { get; set; }

        /// <summary>The control number of the booklet, e.g. "0004501".</summary>
        public string ControlNumber { get; set; }

        public DateTime OrDate { get; set; }

        /// <summary>Who paid. For a resident's document this is the resident;
        /// for a business clearance it is the business.</summary>
        public string PayerName { get; set; }

        public decimal Amount { get; set; }
        public PaymentMethod Method { get; set; }

        /// <summary>The request this collection belongs to, when there is one.
        /// Facility rentals and cedulas may be issued without a request.</summary>
        public int? RequestId { get; set; }

        public string CollectedBy { get; set; }
        public string Remarks { get; set; }

        public bool IsVoid { get; set; }
        public string VoidReason { get; set; }
        public DateTime? VoidedOn { get; set; }
        public string VoidedBy { get; set; }

        public DateTime CreatedOn { get; set; }

        public OfficialReceipt()
        {
            OrNumber = string.Empty;
            SeriesCode = string.Empty;
            ControlNumber = string.Empty;
            PayerName = string.Empty;
            CollectedBy = string.Empty;
            Remarks = string.Empty;
            VoidReason = string.Empty;
            VoidedBy = string.Empty;
            OrDate = DateTime.Today;
            Method = PaymentMethod.Cash;
            CreatedOn = DateTime.Now;
        }

        /// <summary>
        /// Whether the OR number looks like something a real receipt booklet
        /// would print. I do not force one exact format - other barangays
        /// number theirs differently - but I do refuse blanks and I refuse
        /// more than 30 characters, which is what the column holds.
        /// </summary>
        public static bool IsWellFormedOrNumber(string orNumber)
        {
            if (string.IsNullOrWhiteSpace(orNumber)) return false;
            if (orNumber.Trim().Length > 30) return false;
            return orNumber.Trim().Length >= 4;
        }

        public static bool IsWellFormedControlNumber(string controlNumber)
        {
            if (string.IsNullOrWhiteSpace(controlNumber)) return false;
            return controlNumber.Trim().Length <= 30;
        }

        public string GetMethodText()
        {
            return EnumText.Spaced(Method.ToString());
        }

        /// <summary>"OR 2026-000451 (series A / control 0004501) - P100.00".</summary>
        public string Summary()
        {
            string text = "OR " + OrNumber;
            if (!string.IsNullOrWhiteSpace(SeriesCode) || !string.IsNullOrWhiteSpace(ControlNumber))
                text += " (series " + SeriesCode + " / control " + ControlNumber + ")";
            text += " - P" + Amount.ToString("#,##0.00");
            if (IsVoid) text += " [VOID]";
            return text;
        }

        public void Void(string reason, string voidedBy, DateTime when)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new InvalidOperationException("A receipt is only voided with a written reason.");
            if (IsVoid) throw new InvalidOperationException("That receipt is already void.");

            IsVoid = true;
            VoidReason = reason.Trim();
            VoidedOn = when;
            VoidedBy = voidedBy ?? string.Empty;
        }
    }

    /// <summary>
    /// One booklet of official receipts, with the range of control numbers in
    /// it. The administrator records a booklet here when the treasurer hands
    /// it over, and every collection is then written against one of these
    /// ranges - which is why a receipt can never be invented out of nowhere.
    /// </summary>
    public class ReceiptSeries
    {
        public int SeriesId { get; internal set; }

        /// <summary>Short code for the booklet, e.g. "A".</summary>
        public string SeriesCode { get; set; }

        /// <summary>The control numbers in this booklet, e.g. 0004501 to 0004600.</summary>
        public string ControlFrom { get; set; }
        public string ControlTo { get; set; }

        /// <summary>The accountable officer the booklet was issued to.</summary>
        public string IssuedTo { get; set; }

        public DateTime IssuedOn { get; set; }
        public bool IsActive { get; set; }
        public string Remarks { get; set; }

        public ReceiptSeries()
        {
            SeriesCode = string.Empty;
            ControlFrom = string.Empty;
            ControlTo = string.Empty;
            IssuedTo = string.Empty;
            Remarks = string.Empty;
            IssuedOn = DateTime.Today;
            IsActive = true;
        }

        /// <summary>
        /// Is that control number inside this booklet?
        ///
        /// I compare the digits after stripping anything that is not a digit,
        /// but if the values are not numbers at all I fall back to plain text
        /// comparison, so a barangay that writes "A-14" is not locked out.
        /// </summary>
        public bool ContainsControlNumber(string controlNumber)
        {
            if (string.IsNullOrWhiteSpace(controlNumber)) return false;

            long wanted, from, to;
            string wantedDigits = DigitsOnly(controlNumber);
            if (long.TryParse(wantedDigits, out wanted)
                && long.TryParse(DigitsOnly(ControlFrom), out from)
                && long.TryParse(DigitsOnly(ControlTo), out to))
            {
                return wanted >= from && wanted <= to;
            }

            return string.Compare(controlNumber.Trim(), ControlFrom, StringComparison.OrdinalIgnoreCase) >= 0
                && string.Compare(controlNumber.Trim(), ControlTo, StringComparison.OrdinalIgnoreCase) <= 0;
        }

        private static string DigitsOnly(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            System.Text.StringBuilder digits = new System.Text.StringBuilder();
            foreach (char c in text) if (char.IsDigit(c)) digits.Append(c);
            return digits.ToString();
        }

        public string RangeText()
        {
            return ControlFrom + " to " + ControlTo;
        }
    }
}
