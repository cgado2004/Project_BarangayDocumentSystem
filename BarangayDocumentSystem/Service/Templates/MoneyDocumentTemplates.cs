using BarangayDocumentSystem.Models;

namespace BarangayDocumentSystem.Service.Templates;

/// <summary>
/// The Community Tax Certificate - the cedula.
///
/// RA 7160, Sec. 156 fixes the computation for individuals: a basic
/// community tax plus one peso for every thousand pesos of gross annual
/// income from the preceding year, the additional portion capped at five
/// thousand pesos. The computation is printed on the face of the
/// certificate, because a tax receipt nobody can recompute is worth
/// nothing to the person who paid it.
/// </summary>
public sealed class CommunityTaxTemplate : DocumentTemplateBase
{
    public CommunityTaxTemplate() : base(DocumentType.CommunityTaxCertificate) { }

    protected override string Noun => "certificate";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        var r = request.Resident;
        decimal income = Math.Max(0m, request.Input.GrossAnnualIncome);
        decimal additional = request.Fee - 5m;   // whatever is left of the fee is the income portion

        yield return $"    This is to certify that {FormalName(r)}, {r.GetAge()} years of age, " +
                     $"{r.CivilStatus.ToString().ToUpperInvariant()}, {r.Gender}, of " +
                     $"{r.Purok}, {barangay.BarangayName}, {barangay.CityName}, " +
                     $"{barangay.ProvinceName}, occupation " +
                     (string.IsNullOrWhiteSpace(r.Occupation) ? "—" : r.Occupation) + ",";

        yield return "";
        yield return $"    has this day paid the COMMUNITY TAX for the year " +
                     $"{DateTime.Now.Year}, computed on the declarant's own sworn " +
                     "statement of gross income for the preceding year as follows:";

        yield return "";
        yield return $"        Basic community tax ........................... {DisplayFormat.Peso(5m)}";
        yield return $"        Sworn gross annual income ..................... {DisplayFormat.Peso(income)}";
        yield return $"        Additional tax, ₱1.00 per ₱1,000 of income ..... {DisplayFormat.Peso(Math.Max(0m, additional))}";
        yield return $"        TOTAL COMMUNITY TAX PAID ...................... {DisplayFormat.Peso(request.Fee)}";

        yield return "";
        yield return "    Issued pursuant to Section 156 of Republic Act No. 7160 (Local " +
                     "Government Code of 1991). The additional community tax of an " +
                     "individual shall in no case exceed ₱5,000.00.";

        yield return "";
        yield return "    Any person who makes a false statement in the sworn declaration " +
                     "of income shall, upon conviction, suffer the penalties provided by law.";
    }
}

/// <summary>
/// The Katarungang Pambarangay filing - the ₱150 the charter charges to
/// file a case with the Lupong Tagapamayapa, the barangay justice system
/// created by RA 7160, Secs. 399 to 422.
/// </summary>
public sealed class CaseFilingTemplate : DocumentTemplateBase
{
    public CaseFilingTemplate() : base(DocumentType.LuponCaseFiling) { }

    protected override string Noun => "filing";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} has this " +
                     "day filed with the Lupong Tagapamayapa of this barangay a complaint " +
                     "involving the matter described in the stated purpose of this request, " +
                     "and that the filing fee of " + DisplayFormat.Peso(request.Fee) +
                     " under the Barangay Citizen's Charter has been assessed.";

        yield return "";
        yield return "    The parties shall be summoned to appear before the Punong " +
                     "Barangay for mediation, and thereafter before the Pangkat ng " +
                     "Tagapagkasundo if mediation fails, pursuant to the Katarungang " +
                     "Pambarangay Rules. No peace bond shall be required.";
    }
}

/// <summary>
/// Barangay facility use - the covered court, the multipurpose hall and
/// the rest - at the charter rate of ₱200 per hour. The hours and the
/// computation go on the certificate.
/// </summary>
public sealed class FacilityRentalTemplate : DocumentTemplateBase
{
    private readonly FeeSchedule _fees;

    public FacilityRentalTemplate(FeeSchedule fees)
        : base(DocumentType.BarangayFacilityRental)
    {
        _fees = fees;
    }

    protected override string Noun => "permit";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        decimal hours = request.Input.Hours;
        decimal billable = Math.Ceiling(hours);
        string facility = request.Input.Detail.Trim();

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} has been " +
                     "granted permission to use the barangay facility described as " +
                     (facility.Length > 0 ? facility : "the barangay facility stated in the " +
                     "purpose of this request") + ", for the activity stated in the said " +
                     "purpose, subject to existing barangay rules on the use of its " +
                     "facilities.";

        yield return "";
        yield return $"        Hours of use ......... {DisplayFormat.Hours(hours)} hour(s), " +
                     $"billed as {DisplayFormat.Hours(billable)}";
        yield return $"        Rate ................. {DisplayFormat.Peso(_fees.FacilityHourly)} per hour";
        yield return $"        TOTAL FEE ........... {DisplayFormat.Peso(request.Fee)}";
    }
}

/// <summary>
/// The catch-all for any other processing fee the Barangay Taripa prices.
/// The Taripa item and the assessed amount are printed, so the receipt and
/// the certificate always agree about what was charged and why.
/// </summary>
public sealed class TarifaFeeTemplate : DocumentTemplateBase
{
    public TarifaFeeTemplate() : base(DocumentType.OtherTarifaProcessingFee) { }

    protected override string Noun => "official certification";

    protected override IEnumerable<string> Describe(DocumentRequest request, BarangayProfile barangay)
    {
        string item = request.Input.Detail.Trim();

        yield return ResidentIntroduction(request, barangay);
        yield return "";
        yield return $"    This is to certify that {request.Resident.GetFullName()} has this " +
                     "day paid the barangay processing fee assessed under the Barangay " +
                     "Taripa, to wit:";

        yield return "";
        yield return $"        Item ................ {item}";
        yield return $"        Amount assessed ...... {DisplayFormat.Peso(request.Fee)}";

        yield return "";
        yield return "    Issued in connection with the stated purpose of this request, " +
                     "pursuant to the Barangay Citizen's Charter and Section 152 of " +
                     "Republic Act No. 7160.";
    }
}
