// =====================================================================
//  PART:    Models - the barangay's own details, read from App.config
//  ORIGIN:  leader_draft - Clint Wood Gado
//  EDITS:   Clint Wood Gado - header only
//  VOICE:   every comment in this file is mine (Clint), in the first person
// =====================================================================
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
namespace BarangayDocumentSystem.Models;

/// <summary>
/// Who the barangay is.
///
/// Everything printed on a certificate letterhead comes from here, so after
/// an election there is exactly one place to change. Program.cs replaces
/// <see cref="Current"/> with the values from App.config at startup.
/// </summary>
public class BarangayProfile
{
    public string BarangayName   { get; init; } = string.Empty;
    public string CityName       { get; init; } = string.Empty;
    public string ProvinceName   { get; init; } = string.Empty;
    public string PunongBarangay { get; init; } = string.Empty;
    public string OfficeHours    { get; init; } = string.Empty;

    /// <summary>
    /// The profile the app is actually using. Program.cs sets this from
    /// App.config, so every certificate picks up whatever my group-mates
    /// configured without me passing the profile down through every screen.
    /// </summary>
    public static BarangayProfile Current { get; set; }

    /// <summary>
    /// The real barangay I wrote this system for.
    ///
    /// I took the Punong Barangay's name from the City Budget Office Letter
    /// of Review dated 27 November 2024, which records the CY 2025 Annual
    /// Budget as submitted by her on 21 November 2024. My earlier versions
    /// carried a placeholder here, which would have been embarrassing on a
    /// printed certificate. This is the actual name.
    /// </summary>
    public static readonly BarangayProfile MagugpoPoblacion = new()
    {
        BarangayName   = "Barangay Magugpo Poblacion",
        CityName       = "City of Tagum",
        ProvinceName   = "Davao del Norte",
        PunongBarangay = "HON. EUGENIA SOLIS HINGPIT, MD",
        OfficeHours    = "Monday to Friday, 8:00 AM - 5:00 PM"
    };

    /// <summary>
    /// I set the default here rather than in the property initialiser.
    ///
    /// C# runs static field initialisers in the order they appear, so if I
    /// had written "= MagugpoPoblacion" above, it would have run before
    /// MagugpoPoblacion existed and left me with null.
    /// </summary>
    static BarangayProfile() => Current = MagugpoPoblacion;
}
