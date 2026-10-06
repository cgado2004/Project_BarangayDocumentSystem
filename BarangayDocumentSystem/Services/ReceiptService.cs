// ---------------------------------------------------------------------------
//  ReceiptService.cs - the government OR, and the booklet it came from.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using System.Collections.Generic;
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
        /// a receipt, the barangay would have money with nothing behind it.
        /// </summary>
        public OperationResult<OfficialReceipt> Collect(DocumentRequest request, decimal amount,
                                                       string seriesCode, string orNumber, string controlNumber,
                                                       PaymentMethod method, string remarks)
        {
            string refusal;
            if (!Allowed(Permission.CollectPayments, out refusal))
                return OperationResult<OfficialReceipt>.Fail(refusal);

            if (request == null) return OperationResult<OfficialReceipt>.Fail("Please choose a request first.");

            if (request.IsPaid)
                return OperationResult<OfficialReceipt>.Fail(
                    "That request was already paid against OR " + request.OfficialReceiptNumber + ".");

            if (request.Status == RequestStatus.Rejected)
                return OperationResult<OfficialReceipt>.Fail("That request was rejected, so no fee is due on it.");

            ReceiptSeries series = FindSeriesForControlNumber(controlNumber);

            var problems = InputValidator.ValidatePayment(amount, orNumber, controlNumber,
                request.Fee, seriesCode, series);
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
            receipt.PayerName = request.ResidentName;
            receipt.Amount = amount;
            receipt.Method = method;
            receipt.RequestId = request.RequestId;
            receipt.CollectedBy = _session.Username;
            receipt.Remarks = remarks == null ? string.Empty : remarks.Trim();
            receipt.CreatedOn = now;

            _repository.InsertReceipt(receipt);

            request.RecordPayment(amount, receipt.OrNumber, receipt.ControlNumber, _session.Username, now);
            _repository.UpdateRequest(request);

            _log.RecordCollection(receipt, request.ResidentName);

            string message = "Collected P" + amount.ToString("#,##0.00") + " against OR " + receipt.OrNumber
                           + " (control " + receipt.ControlNumber + ").";

            if (request.Status == RequestStatus.Cleared)
                message += " The request can be prepared for release now.";

            return OperationResult<OfficialReceipt>.Ok(receipt, message);
        }

        /// <summary>
        /// Collects for something that has no request behind it - a cedula at
        /// the counter, a facility rental paid in cash on the day. The receipt
        /// is the same kind of receipt; only the link to a request is empty.
        /// </summary>
        public OperationResult<OfficialReceipt> CollectStandalone(string payerName, decimal amount,
                                                                 string seriesCode, string orNumber,
                                                                 string controlNumber, PaymentMethod method,
                                                                 string remarks)
        {
            string refusal;
            if (!Allowed(Permission.CollectPayments, out refusal))
                return OperationResult<OfficialReceipt>.Fail(refusal);

            ReceiptSeries series = FindSeriesForControlNumber(controlNumber);

            var problems = InputValidator.ValidatePayment(amount, orNumber, controlNumber, amount, seriesCode, series);
            if (problems.Count > 0)
                return OperationResult<OfficialReceipt>.Fail(string.Join(Environment.NewLine, problems.ToArray()));

            if (_repository.ReceiptNumberExists(seriesCode, orNumber, 0))
                return OperationResult<OfficialReceipt>.Fail(
                    "OR " + orNumber.Trim() + " in series " + seriesCode.Trim() + " is already recorded.");

            DateTime now = _clock.Now();

            OfficialReceipt receipt = new OfficialReceipt();
            receipt.OrNumber = orNumber.Trim();
            receipt.SeriesCode = seriesCode.Trim();
            receipt.ControlNumber = controlNumber.Trim();
            receipt.OrDate = now.Date;
            receipt.PayerName = payerName == null ? string.Empty : payerName.Trim();
            receipt.Amount = amount;
            receipt.Method = method;
            receipt.CollectedBy = _session.Username;
            receipt.Remarks = remarks == null ? string.Empty : remarks.Trim();
            receipt.CreatedOn = now;

            _repository.InsertReceipt(receipt);
            _log.RecordCollection(receipt, payerName);

            return OperationResult<OfficialReceipt>.Ok(receipt,
                "Collected P" + amount.ToString("#,##0.00") + " against OR " + receipt.OrNumber + ".");
        }

        /// <summary>
        /// Corrects a mistake. Voiding keeps the receipt and its number in the
        /// register, marked as void with the reason - which is what a real
        /// collection register does, and what stops a number from quietly
        /// disappearing from the booklet.
        /// </summary>
        public OperationResult Void(OfficialReceipt receipt, string reason)
        {
            string refusal;
            if (!Allowed(Permission.VoidReceipts, out refusal)) return OperationResult.Fail(refusal);

            if (receipt == null) return OperationResult.Fail("Please choose a receipt first.");

            try
            {
                receipt.Void(reason, _session.Username, _clock.Now());
            }
            catch (InvalidOperationException error)
            {
                return OperationResult.Fail(error.Message);
            }

            _repository.UpdateReceipt(receipt);

            // If the receipt was for a request, the request goes back to
            // unpaid. Otherwise the release guard would be satisfied by a
            // receipt that no longer stands.
            if (receipt.RequestId.HasValue)
            {
                DocumentRequest request = _repository.GetRequest(receipt.RequestId.Value);
                if (request != null && string.Equals(request.OfficialReceiptNumber, receipt.OrNumber,
                        StringComparison.OrdinalIgnoreCase))
                {
                    request.ClearPaymentForVoidedReceipt(_session.Username, _clock.Now());
                    _repository.UpdateRequest(request);
                }
            }

            _log.Record(ActivityModule.Payments, "Voided", "Official receipt", receipt.OrNumber,
                "Voided OR " + receipt.OrNumber + " (control " + receipt.ControlNumber + ") for P"
                + receipt.Amount.ToString("#,##0.00") + ". Reason: " + reason);

            return OperationResult.Ok("OR " + receipt.OrNumber + " is void. The number stays in the register.");
        }

        // ==================================================================
        //  Booklets
        // ==================================================================

        public OperationResult<ReceiptSeries> AddSeries(string seriesCode, string controlFrom, string controlTo,
                                                       string issuedTo)
        {
            string refusal;
            if (!Allowed(Permission.ManageReceiptSeries, out refusal))
                return OperationResult<ReceiptSeries>.Fail(refusal);

            if (string.IsNullOrWhiteSpace(seriesCode))
                return OperationResult<ReceiptSeries>.Fail("The booklet needs a series code, like A or B.");

            if (string.IsNullOrWhiteSpace(controlFrom) || string.IsNullOrWhiteSpace(controlTo))
                return OperationResult<ReceiptSeries>.Fail(
                    "Please write the first and last control number printed in the booklet.");

            foreach (ReceiptSeries existing in _repository.GetReceiptSeries(false))
                if (string.Equals(existing.SeriesCode, seriesCode.Trim(), StringComparison.OrdinalIgnoreCase))
                    return OperationResult<ReceiptSeries>.Fail("Series " + seriesCode.Trim() + " is already recorded.");

            ReceiptSeries series = new ReceiptSeries();
            series.SeriesCode = seriesCode.Trim();
            series.ControlFrom = controlFrom.Trim();
            series.ControlTo = controlTo.Trim();
            series.IssuedTo = issuedTo == null ? string.Empty : issuedTo.Trim();
            series.IssuedOn = _clock.Now().Date;
            series.IsActive = true;

            _repository.InsertReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Recorded booklet", "Receipt series", series.SeriesCode,
                "Recorded receipt booklet " + series.SeriesCode + " covering control numbers "
                + series.RangeText() + (string.IsNullOrWhiteSpace(series.IssuedTo)
                    ? "." : ", issued to " + series.IssuedTo + "."));

            return OperationResult<ReceiptSeries>.Ok(series,
                "Booklet " + series.SeriesCode + " is recorded. Collections can now be written against it.");
        }

        public OperationResult CloseSeries(ReceiptSeries series)
        {
            string refusal;
            if (!Allowed(Permission.ManageReceiptSeries, out refusal)) return OperationResult.Fail(refusal);
            if (series == null) return OperationResult.Fail("Please choose a booklet first.");

            series.IsActive = false;
            _repository.UpdateReceiptSeries(series);

            _log.Record(ActivityModule.Payments, "Closed booklet", "Receipt series", series.SeriesCode,
                "Closed receipt booklet " + series.SeriesCode + " (" + series.RangeText() + ").");

            return OperationResult.Ok("Booklet " + series.SeriesCode + " is closed. Its receipts stay in the register.");
        }

        // ==================================================================
        //  Internals
        // ==================================================================

        private bool Allowed(Permission permission, out string refusal)
        {
            refusal = string.Empty;

            if (_session == null || !_session.IsSignedIn)
            {
                refusal = "Please sign in first.";
                return false;
            }

            if (_session.Has(permission)) return true;

            refusal = _session.RefusalFor(permission);
            _log.Record(ActivityModule.Payments, "Refused", "Official receipt", string.Empty,
                _session.DescribeForLog(permission));
            return false;
        }

        private static string DigitsOf(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;

            System.Text.StringBuilder digits = new System.Text.StringBuilder();
            foreach (char c in text) if (char.IsDigit(c)) digits.Append(c);
            return digits.ToString();
        }
    }
}
