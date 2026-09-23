using System;
using System.Data;
using System.Data.SqlClient;
using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Data
{
    internal static class SqlRequestMapping
    {
        internal const string Columns =
            "ResidentId, DocumentType, DocumentName, " +
            "Purpose, BusinessName, BusinessAddress, " +
            "BusinessNature, DateRequested, DateReleased, " +
            "Status, Fee, FeeBasis, " +
            "IsPaid, OfficialReceiptNumber, DatePaid, " +
            "RejectionReason, ReleasedDocumentText";
        internal const string Parameters =
            "@ResidentId, @DocumentType, @DocumentName, " +
            "@Purpose, @BusinessName, @BusinessAddress, " +
            "@BusinessNature, @DateRequested, @DateReleased, " +
            "@Status, @Fee, @FeeBasis, " +
            "@IsPaid, @OfficialReceiptNumber, @DatePaid, " +
            "@RejectionReason, @ReleasedDocumentText";
        internal const string Assignments =
            "DocumentType = @DocumentType, DocumentName = @DocumentName, Purpose = @Purpose, " +
            "BusinessName = @BusinessName, BusinessAddress = @BusinessAddress, BusinessNature = @BusinessNature, " +
            "DateRequested = @DateRequested, DateReleased = @DateReleased, Status = @Status, " +
            "Fee = @Fee, FeeBasis = @FeeBasis, IsPaid = @IsPaid, " +
            "OfficialReceiptNumber = @OfficialReceiptNumber, DatePaid = @DatePaid, RejectionReason = @RejectionReason, " +
            "ReleasedDocumentText = @ReleasedDocumentText";

        internal static void AddParameters(SqlCommand command, DocumentRequest record)
        {
            command.Parameters.Add("@ResidentId", SqlDbType.Int).Value = record.ResidentId;
            command.Parameters.Add("@DocumentType", SqlDbType.Int).Value = (int)record.DocumentType;
            command.Parameters.Add("@DocumentName", SqlDbType.NVarChar, 120).Value = record.DocumentName ?? "";
            command.Parameters.Add("@Purpose", SqlDbType.NVarChar, 300).Value = record.Purpose ?? "";
            command.Parameters.Add("@BusinessName", SqlDbType.NVarChar, 120).Value = record.BusinessName ?? "";
            command.Parameters.Add("@BusinessAddress", SqlDbType.NVarChar, 250).Value = record.BusinessAddress ?? "";
            command.Parameters.Add("@BusinessNature", SqlDbType.NVarChar, 150).Value = record.BusinessNature ?? "";
            command.Parameters.Add("@DateRequested", SqlDbType.DateTime2).Value = record.DateRequested;
            command.Parameters.Add("@DateReleased", SqlDbType.DateTime2).Value = (object)record.DateReleased ?? DBNull.Value;
            command.Parameters.Add("@Status", SqlDbType.Int).Value = (int)record.Status;
            var fee = command.Parameters.Add("@Fee", SqlDbType.Decimal);
            fee.Precision = 12;
            fee.Scale = 2;
            fee.Value = record.Fee;
            command.Parameters.Add("@FeeBasis", SqlDbType.NVarChar, 1000).Value = record.FeeBasis ?? "";
            command.Parameters.Add("@IsPaid", SqlDbType.Bit).Value = record.IsPaid;
            command.Parameters.Add("@OfficialReceiptNumber", SqlDbType.NVarChar, 50).Value = (object)(string.IsNullOrWhiteSpace(record.OfficialReceiptNumber) ? null : record.OfficialReceiptNumber) ?? DBNull.Value;
            command.Parameters.Add("@DatePaid", SqlDbType.DateTime2).Value = (object)record.DatePaid ?? DBNull.Value;
            command.Parameters.Add("@RejectionReason", SqlDbType.NVarChar, 300).Value = record.RejectionReason ?? "";
            command.Parameters.Add("@ReleasedDocumentText", SqlDbType.NVarChar, -1).Value = record.ReleasedDocumentText ?? "";
        }

        internal static DocumentRequest Read(SqlDataReader reader)
        {
            return new DocumentRequest
            {
                RequestId = (int)reader["RequestId"],
                Version = (int)reader["Version"],
                ResidentSnapshot = SqlResidentMapping.Read(reader, true),
                ResidentId = (int)reader["ResidentId"],
                DocumentType = (DocumentType)(int)reader["DocumentType"],
                DocumentName = (string)reader["DocumentName"],
                Purpose = (string)reader["Purpose"],
                BusinessName = (string)reader["BusinessName"],
                BusinessAddress = (string)reader["BusinessAddress"],
                BusinessNature = (string)reader["BusinessNature"],
                DateRequested = (DateTime)reader["DateRequested"],
                DateReleased = reader["DateReleased"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["DateReleased"],
                Status = (RequestStatus)(int)reader["Status"],
                Fee = (decimal)reader["Fee"],
                FeeBasis = (string)reader["FeeBasis"],
                IsPaid = (bool)reader["IsPaid"],
                OfficialReceiptNumber = reader["OfficialReceiptNumber"] == DBNull.Value ? "" : (string)reader["OfficialReceiptNumber"],
                DatePaid = reader["DatePaid"] == DBNull.Value ? (DateTime?)null : (DateTime)reader["DatePaid"],
                RejectionReason = (string)reader["RejectionReason"],
                ReleasedDocumentText = (string)reader["ReleasedDocumentText"]
            };
        }
    }
}
