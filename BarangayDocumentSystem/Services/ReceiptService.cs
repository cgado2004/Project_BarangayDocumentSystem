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
            string orNumber, string controlNumber, string seriesCode,
            decimal amountTendered, PaymentMethod method, string remarks)
        {
            if (request == null) return OperationResult<OfficialReceipt>.Fail("There is no request to collect for.");

            if (request.Fee <= 0m)
                return OperationResult<OfficialReceipt>.Fail(
                    "This document is free, so there is nothing to collect. Release it directly.");

            if (request.IsPaid)
                return OperationResult<OfficialReceipt>.Fail(
                    "The fee for " + request.ReferenceNumber + " was already collected against OR "
                    + request.OfficialReceiptNumber + ".");

            ReceiptSeries matchedSeries = FindSeriesForControlNumber(controlNumber);
            var problems = InputValidator.ValidatePayment(amountTendered, orNumber, controlNumber,
                request.Fee, seriesCode, matchedSeries);
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
            if (receipt.RequestId > 0)
            {
                DocumentRequest request = _repository.GetRequest(receipt.RequestId.Value);
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
        //  Walk-in collections, with no request behind them
        // ==================================================================

        /// <summary>
        /// Writes a receipt for money handed over at the counter with no
        /// document request behind it - a rental paid in cash, a contribution,
        /// anything the treasurer needs an OR for.
        ///
        /// The same rules hold as when a request is settled: the numbers are
        /// compulsory, the control number has to sit inside a real booklet,
        /// and the same OR number cannot be written twice in one booklet.
        /// </summary>
        public OperationResult<OfficialReceipt> CollectStandalone(string payerName, decimal amount,
            string seriesCode, string orNumber, string controlNumber,
            PaymentMethod method, string remarks)
        {
            if (string.IsNullOrWhiteSpace(payerName))
                return OperationResult<OfficialReceipt>.Fail("Please write who handed over the money.");

            if (string.IsNullOrWhiteSpace(seriesCode))
                return OperationResult<OfficialReceipt>.Fail("Please choose the booklet the receipt was written in.");

            ReceiptSeries matchedSeries = FindSeriesForControlNumber(controlNumber);
            var problems = InputValidator.ValidatePayment(amount, orNumber, controlNumber,
                0m, seriesCode, matchedSeries);
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
            receipt.Amount = amount;
            receipt.Method = method;
            receipt.RequestId = null;
            receipt.CollectedBy = _session.Username;
            receipt.Remarks = remarks ?? string.Empty;
            receipt.CreatedOn = now;

            _repository.InsertReceipt(receipt);

            _log.Record(ActivityModule.Payments, "Collected " + amount.ToString("#,##0.00"),
                "Official receipt", receipt.OrNumber,
                "OR " + receipt.OrNumber + " (series " + receipt.SeriesCode
                + ", control " + receipt.ControlNumber + ") from " + receipt.PayerName
                + ", no request behind it.");

            return OperationResult<OfficialReceipt>.Ok(receipt,
                "Collected " + amount.ToString("#,##0.00") + " against OR " + receipt.OrNumber + ".");
        }

        // ==================================================================
        //  The booklets themselves
        // ==================================================================

        /// <summary>
        /// Records a receipt booklet the barangay was issued, so that control
        /// numbers typed at the counter can be checked against something real.
        /// </summary>
        public OperationResult<ReceiptSeries> AddSeries(string seriesCode, string controlFrom,
            string controlTo, string issuedTo)
        {
            string refusal;
            if (!Allowed(out refusal))
                return OperationResult<ReceiptSeries>.Fail(refusal);

            if (string.IsNullOrWhiteSpace(seriesCode))
                return OperationResult<ReceiptSeries>.Fail("Please copy the series code from the cover of the booklet.");
            if (string.IsNullOrWhiteSpace(controlFrom) || string.IsNullOrWhiteSpace(controlTo))
                return OperationResult<ReceiptSeries>.Fail("Both the first and the last control number are needed.");

            string code = seriesCode.Trim();
            foreach (ReceiptSeries existing in _repository.GetReceiptSeries(false))
            {
                if (string.Equals(existing.SeriesCode, code, StringComparison.OrdinalIgnoreCase))
                    return OperationResult<ReceiptSeries>.Fail(
                        "Booklet " + code + " is already recorded (" + existing.RangeText() + ").");
            }

            ReceiptSeries series = new ReceiptSeries();
            series.SeriesCode = code;
            series.ControlFrom = controlFrom.Trim();
            series.ControlTo = controlTo.Trim();
            series.IssuedTo = string.IsNullOrWhiteSpace(issuedTo) ? string.Empty : issuedTo.Trim();
            series.IssuedOn = _clock.Now();
            series.IsActive = true;

            _repository.InsertReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Booklet recorded", "Receipt series", series.SeriesCode,
                "Booklet " + series.SeriesCode + " (" + series.RangeText() + ") recorded as issued to "
                + (string.IsNullOrEmpty(series.IssuedTo) ? "the barangay" : series.IssuedTo) + ".");

            return OperationResult<ReceiptSeries>.Ok(series,
                "Booklet " + series.SeriesCode + " (" + series.RangeText() + ") is recorded.");
        }

        /// <summary>
        /// Closes a finished booklet. Nothing already written in it changes;
        /// the counter simply stops suggesting its numbers.
        /// </summary>
        public OperationResult CloseSeries(ReceiptSeries series)
        {
            string refusal;
            if (!Allowed(out refusal))
                return OperationResult.Fail(refusal);

            if (series == null) return OperationResult.Fail("Please choose the booklet to close.");

            if (!series.IsActive)
                return OperationResult.Fail("Booklet " + series.SeriesCode + " is already closed.");

            series.IsActive = false;
            _repository.UpdateReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Booklet closed", "Receipt series", series.SeriesCode,
                "Booklet " + series.SeriesCode + " (" + series.RangeText() + ") was closed.");

            return OperationResult.Ok("Booklet " + series.SeriesCode + " is closed.");
        }

        private bool Allowed(out string refusal)
        {
            refusal = string.Empty;
            if (_session == null || _session.User == null)
            {
                refusal = "Please sign in first.";
                return false;
            }

            if (!PermissionSet.Allows(_session.User.Role, Permission.ManageReceiptSeries))
            {
                refusal = "Your account is not allowed to change receipt booklets. Ask the administrator if you need this.";
                return false;
            }

            return true;
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
