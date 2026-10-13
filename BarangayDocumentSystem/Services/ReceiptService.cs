// ---------------------------------------------------------------------------
//  ReceiptService.cs - the government OR, and the booklet it came from.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using BarangayDocumentSystem.Interfaces;
using BarangayDocumentSystem.Models;
using BarangayDocumentSystem.Security;

namespace BarangayDocumentSystem.Services
{
    /// <summary>
    /// Takes money in, and writes it down the way the barangay treasurer
    /// expects.
    ///
    /// The whole point of this class is that a peso cannot enter the system
    /// without an official receipt and a control number. Those two numbers are
    /// what makes a collection traceable: the OR number is what the resident
    /// holds, and the control number says which booklet it was written in,
    /// which is exactly the question an audit asks.
    ///
    /// So the rules are:
    ///
    ///  * the receipt number and the control number are both compulsory;
    ///  * the same OR number cannot be written twice in the same booklet
    ///    (the check here, plus a unique key in the database);
    ///  * the control number has to fall inside a booklet that was actually
    ///    issued to the barangay - no receipt invented from nowhere;
    ///  * the amount cannot be less than the assessed fee;
    ///  * a mistake is fixed by voiding with a reason and writing a new
    ///    receipt, never by editing or deleting the old one.
    /// </summary>
    public class ReceiptService
    {
        private readonly IBarangayRepository _repository;
        private readonly ActivityLogService _log;
        private readonly SessionManager _session;
        private readonly IClock _clock;

        public ReceiptService(IBarangayRepository repository, ActivityLogService log,
                              SessionManager session, IClock clock)
        {
            if (repository == null) throw new ArgumentNullException("repository");

            _repository = repository;
            _log = log;
            _session = session;
            _clock = clock == null ? new SystemClock() : clock;
        }

        // ==================================================================
        //  Reading
        // ==================================================================

        public IList<OfficialReceipt> GetReceipts(ReceiptQuery query)
        {
            return _repository.GetReceipts(query);
        }

        public IList<ReceiptSeries> GetSeries(bool activeOnly)
        {
            return _repository.GetReceiptSeries(activeOnly);
        }

        /// <summary>
        /// The next number after the last one written in that booklet.
        ///
        /// I offer it as a suggestion in the payment box so the clerk does not
        /// have to read the last receipt in the booklet and add one in her
        /// head. She can still type over it - the number printed on the paper
        /// is what counts, and the paper is the only authority.
        /// </summary>
        public string SuggestNextOrNumber(string seriesCode)
        {
            if (string.IsNullOrWhiteSpace(seriesCode)) return string.Empty;

            ReceiptQuery query = new ReceiptQuery();
            query.Keyword = string.Empty;

            int highest = 0;
            string prefix = string.Empty;

            foreach (OfficialReceipt receipt in _repository.GetReceipts(query))
            {
                if (!string.Equals(receipt.SeriesCode, seriesCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;

                string digits = DigitsOf(receipt.OrNumber);
                int parsed;
                if (digits.Length == 0 || !int.TryParse(digits, out parsed)) continue;

                if (parsed > highest)
                {
                    highest = parsed;
                    prefix = receipt.OrNumber.Substring(0, receipt.OrNumber.Length - digits.Length);
                }
            }

            if (highest == 0)
            {
                // Nothing collected yet in this booklet: I start from the
                // booklet's own first control number, which is the honest
                // answer.
                ReceiptSeries series = FindSeries(seriesCode);
                if (series == null) return string.Empty;

                string digits = DigitsOf(series.ControlFrom);
                return string.IsNullOrEmpty(digits) ? series.ControlFrom : digits;
            }

            return prefix + (highest + 1).ToString(new string('0', Math.Max(1, DigitsOf(prefix + highest).Length)));
        }

        /// <summary>Which booklet covers that control number. The payment
        /// screen uses it to warn before the clerk finishes typing.</summary>
        public ReceiptSeries FindSeriesForControlNumber(string controlNumber)
        {
            return _repository.FindSeriesForControlNumber(controlNumber);
        }

        private ReceiptSeries FindSeries(string seriesCode)
        {
            foreach (ReceiptSeries series in _repository.GetReceiptSeries(false))
                if (string.Equals(series.SeriesCode, seriesCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    return series;

            return null;
        }

        // ==================================================================
        //  Booklets
        // ==================================================================

        /// <summary>
        /// Records a new booklet of official receipts when the treasurer hands
        /// it over, so every collection can be traced back to a booklet.
        ///
        /// The series code has to be new: two booklets with the same code
        /// would make a receipt number ambiguous, and an audit cannot have
        /// that. The control numbers are copied exactly as printed on the
        /// cover, because the paper is the authority.
        /// </summary>
        public OperationResult<ReceiptSeries> AddSeries(string seriesCode, string controlFrom,
                                                        string controlTo, string issuedTo)
        {
            if (string.IsNullOrWhiteSpace(seriesCode))
                return OperationResult<ReceiptSeries>.Fail("The series code of the booklet is required.");
            if (string.IsNullOrWhiteSpace(controlFrom) || string.IsNullOrWhiteSpace(controlTo))
                return OperationResult<ReceiptSeries>.Fail("Both control numbers on the cover are required.");

            string code = seriesCode.Trim();

            if (FindSeries(code) != null)
                return OperationResult<ReceiptSeries>.Fail("There is already a booklet with the series code "
                    + code + ". A series code can only be used once.");

            ReceiptSeries series = new ReceiptSeries();
            series.SeriesCode = code;
            series.ControlFrom = controlFrom.Trim();
            series.ControlTo = controlTo.Trim();
            series.IssuedTo = (issuedTo ?? string.Empty).Trim();
            series.IssuedOn = _clock.Now().Date;
            series.IsActive = true;
            series.SeriesId = _repository.InsertReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Recorded booklet " + code,
                "Receipt series", code,
                "Covers control numbers " + series.ControlFrom + " to " + series.ControlTo
                + ", issued to " + (series.IssuedTo.Length == 0 ? "the barangay" : series.IssuedTo) + ".");

            return OperationResult<ReceiptSeries>.Ok(series,
                "Booklet " + code + " is recorded. It covers control numbers "
                + series.ControlFrom + " to " + series.ControlTo + ".");
        }

        /// <summary>
        /// Closes a booklet so it takes no more collections. Nothing already
        /// written against it is touched - a closed booklet stays in the
        /// register exactly as it is, because the receipts point back at it.
        /// </summary>
        public OperationResult CloseSeries(ReceiptSeries series)
        {
            if (series == null)
                return OperationResult.Fail("Please choose a booklet from the list first.");
            if (!series.IsActive)
                return OperationResult.Fail("Booklet " + series.SeriesCode + " is already closed.");

            series.IsActive = false;
            _repository.UpdateReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Closed booklet " + series.SeriesCode,
                "Receipt series", series.SeriesCode,
                "The booklet (" + series.RangeText() + ") takes no more collections.");

            return OperationResult.Ok("Booklet " + series.SeriesCode + " is closed. It takes no more collections.");
        }

        // ==================================================================
        //  Collecting
        // ==================================================================

        /// <summary>
        /// Records the collection for a request, and marks the request paid, in
        /// one go.
        ///
        /// I keep both halves together on purpose. If the receipt were written
        /// but the request left unpaid, the release would be blocked forever
        /// and the clerk would call me; if the request were marked paid without
        /// the receipt, the money would be untraceable. Together or not at all.
        /// </summary>
        public OperationResult<OfficialReceipt> Collect(DocumentRequest request,
            decimal amountTendered, string seriesCode, string orNumber, string controlNumber,
            PaymentMethod method, string remarks)
        {
            if (request == null) return OperationResult<OfficialReceipt>.Fail("There is no request to collect for.");

            if (request.Fee <= 0m)
                return OperationResult<OfficialReceipt>.Fail(
                    "This document is free, so there is nothing to collect. Release it directly.");

            if (request.IsPaid)
                return OperationResult<OfficialReceipt>.Fail(
                    "The fee for " + request.ReferenceNumber + " was already collected against OR "
                    + request.OfficialReceiptNumber + ".");

            var problems = InputValidator.ValidatePayment(amountTendered, orNumber, controlNumber,
                request.Fee, seriesCode, FindSeriesForControlNumber(controlNumber));
            if (problems.Count > 0)
                return OperationResult<OfficialReceipt>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            if (_repository.ReceiptNumberExists(seriesCode, orNumber, 0))
                return OperationResult<OfficialReceipt>.Fail(
                    "OR " + orNumber.Trim() + " in series " + seriesCode.Trim()
                    + " is already recorded. Please check the receipt in your hand.");

            DateTime now = _clock.Now();

            OfficialReceipt receipt = new OfficialReceipt();
            receipt.OrNumber = orNumber.Trim();
            receipt.SeriesCode = seriesCode.Trim();
            receipt.ControlNumber = controlNumber.Trim();
            receipt.OrDate = now.Date;
            receipt.PayerName = request.ResidentName ?? string.Empty;
            receipt.Amount = amountTendered;
            receipt.Method = method;
            receipt.RequestId = request.RequestId;
            receipt.CollectedBy = _session.Username;
            receipt.Remarks = remarks ?? string.Empty;
            receipt.CreatedOn = now;

            int receiptId = _repository.InsertReceipt(receipt);

            request.RecordPayment(amountTendered, orNumber.Trim(), controlNumber.Trim(), _session.Username, now);
            _repository.UpdateRequest(request);

            _log.Record(ActivityModule.Payments, "Collected " + amountTendered.ToString("#,##0.00"),
                "Document request", request.ReferenceNumber,
                "OR " + orNumber.Trim() + " (series " + seriesCode.Trim()
                + ", control " + controlNumber.Trim() + ") for " + request.GetDocumentName()
                + ". Tendered by " + request.ResidentName + ".");

            return OperationResult<OfficialReceipt>.Ok(receipt,
                "Collected " + amountTendered.ToString("#,##0.00") + " against OR " + orNumber.Trim() + ".");
        }

        /// <summary>
        /// A collection at the counter that does not belong to any document
        /// request - the odd fees the barangay collects that have no paper of
        /// their own.
        ///
        /// It is still a government receipt: the booklet, the OR number and
        /// the control number are all compulsory, exactly as for a request,
        /// so the money lands in the same register and the same audit.
        /// </summary>
        public OperationResult<OfficialReceipt> CollectStandalone(string payerName,
            decimal amountTendered, string seriesCode, string orNumber, string controlNumber,
            PaymentMethod method, string remarks)
        {
            if (string.IsNullOrWhiteSpace(payerName))
                return OperationResult<OfficialReceipt>.Fail(
                    "Please tell whose money this is - the payer is written on the receipt.");

            var problems = InputValidator.ValidatePayment(amountTendered, orNumber, controlNumber,
                0m, seriesCode, FindSeriesForControlNumber(controlNumber));
            if (problems.Count > 0)
                return OperationResult<OfficialReceipt>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            if (_repository.ReceiptNumberExists(seriesCode, orNumber, 0))
                return OperationResult<OfficialReceipt>.Fail(
                    "OR " + orNumber.Trim() + " in series " + seriesCode.Trim()
                    + " is already recorded. Please check the receipt in your hand.");

            DateTime now = _clock.Now();

            OfficialReceipt receipt = new OfficialReceipt();
            receipt.OrNumber = orNumber.Trim();
            receipt.SeriesCode = seriesCode.Trim();
            receipt.ControlNumber = controlNumber.Trim();
            receipt.OrDate = now.Date;
            receipt.PayerName = payerName.Trim();
            receipt.Amount = amountTendered;
            receipt.Method = method;
            receipt.RequestId = 0;
            receipt.CollectedBy = _session.Username;
            receipt.Remarks = remarks ?? string.Empty;
            receipt.CreatedOn = now;

            int receiptId = _repository.InsertReceipt(receipt);

            _log.Record(ActivityModule.Payments, "Collected " + amountTendered.ToString("#,##0.00"),
                "Official receipt", receipt.OrNumber,
                "Standalone collection from " + receipt.PayerName
                + " (series " + receipt.SeriesCode + ", control " + receipt.ControlNumber + ")."
                + (string.IsNullOrWhiteSpace(remarks) ? string.Empty : " " + remarks.Trim()));

            return OperationResult<OfficialReceipt>.Ok(receipt,
                "Collected " + amountTendered.ToString("#,##0.00") + " from " + receipt.PayerName
                + " against OR " + receipt.OrNumber + ".");
        }

        // ==================================================================
        //  Voiding
        // ==================================================================

        /// <summary>
        /// Cancels a receipt that was written wrong, with the reason kept on
        /// the register.
        ///
        /// A voided receipt is not deleted and it is not edited - it stays
        /// exactly as it was written, marked void, with the reason and who
        /// voided it. That is what an audit trail means: the mistake and the
        /// correction are both visible.
        /// </summary>
        public OperationResult Void(OfficialReceipt receipt, string reason)
        {
            if (receipt == null) return OperationResult.Fail("There is no receipt to void.");

            if (receipt.IsVoid)
                return OperationResult.Fail("OR " + receipt.OrNumber + " is already void.");

            if (string.IsNullOrWhiteSpace(reason))
                return OperationResult.Fail("I need a reason before I can void a receipt.");

            DateTime now = _clock.Now();
            receipt.Void(reason.Trim(), _session.Username, now);
            _repository.UpdateReceipt(receipt);

            // If the receipt paid for a request, the request goes back to
            // unpaid so it cannot be released against a dead receipt.
            if (receipt.RequestId.GetValueOrDefault() > 0)
            {
                DocumentRequest request = _repository.GetRequest(receipt.RequestId.GetValueOrDefault());
                if (request != null && request.IsPaid
                    && string.Equals(request.OfficialReceiptNumber, receipt.OrNumber, StringComparison.OrdinalIgnoreCase))
                {
                    request.ClearPaymentForVoidedReceipt(_session.Username, now);
                    _repository.UpdateRequest(request);
                }
            }

            _log.Record(ActivityModule.Payments, "Voided OR " + receipt.OrNumber,
                "Official receipt", receipt.OrNumber,
                "Voided because: " + reason.Trim() + ".");

            return OperationResult.Ok("OR " + receipt.OrNumber + " is void.");
        }

        // ==================================================================
        //  Small helpers
        // ==================================================================

        private static string DigitsOf(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            int start = text.Length;
            while (start > 0 && char.IsDigit(text[start - 1])) start--;
            return text.Substring(start);
        }
    }
}
