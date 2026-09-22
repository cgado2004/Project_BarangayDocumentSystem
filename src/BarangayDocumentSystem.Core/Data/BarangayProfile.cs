namespace BarangayDocumentSystem.Core.Data;

/// <summary>
/// Who the barangay is.
///
/// Everything printed on a certificate letterhead comes from here, so after an
/// election there is exactly one place for me to change.
/// </summary>
public class BarangayProfile
{
    public string BarangayName   { get; init; } = string.Empty;
    public string CityName       { get; init; } = string.Empty;
    public string ProvinceName   { get; init; } = string.Empty;
    public string PunongBarangay { get; init; } = string.Empty;
    public string OfficeHours    { get; init; } = string.Empty;

    /// <summary>
    /// The real barangay I wrote this system for.
    ///
    /// I took the Punong Barangay's name from the City Budget Office Letter of
    /// Review dated 27 November 2024, which records the CY 2025 Annual Budget
    /// as submitted by her on 21 November 2024. My earlier versions carried a
    /// placeholder here, which would have been embarrassing on a printed
    /// certificate. This is the actual name.
    /// </summary>
    /// <summary>
    /// The profile the app is actually using. Program.cs replaces this with
    /// the values from App.config at startup, so every certificate picks up
    /// whatever my group-mates set there without me passing the profile down
    /// through every screen.
    /// </summary>
    public static BarangayProfile Current { get; set; } = null!;

    public static readonly BarangayProfile MagugpoPoblacion = new()
    {
        BarangayName   = "Barangay Magugpo Poblacion",
        CityName       = "City of Tagum",
        ProvinceName   = "Davao del Norte",
        PunongBarangay = "HON. EUGENIA SOLIS HINGPIT, MD",
        OfficeHours    = "Monday to Friday, 8:00 AM - 5:00 PM"
    };

    /// <summary>
    /// The puroks of Magugpo Poblacion.
    ///
    /// I took these from the barangay's own "List of Projects Chargeable
    /// Against the 20% Development Fund - FY 2025", so they are the real names
    /// rather than the "Purok 1, Purok 2, ..." placeholders I used in my
    /// earlier version.
    /// </summary>
    /// <summary>
    /// I set the default here rather than in the property initialiser.
    ///
    /// C# runs static field initialisers in the order they appear, so if I had
    /// written "= MagugpoPoblacion" above, it would have run before
    /// MagugpoPoblacion existed and left me with null.
    /// </summary>
    static BarangayProfile() => Current = MagugpoPoblacion;

    public static readonly string[] Puroks =
    {
        "Purok Arellano",
        "Purok Calachuchi",
        "Purok Cristo Rey",
        "Purok Dagohoy",
        "Purok Lapu-Lapu",
        "Purok Marilag 2",
        "Purok Orchids",
        "Purok Paraiso",
        "Purok Sampaguita",
        "Purok Sulgreg",
        "Purok Sunflower",
        "Purok Talisay",
        "Purok Tandang Sora",
        "Purok Tindalo"
    };
}
