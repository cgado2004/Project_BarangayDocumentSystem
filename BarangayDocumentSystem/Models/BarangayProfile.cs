// ---------------------------------------------------------------------------
//  BarangayProfile.cs - who the barangay is, for the letterhead.
//  Mine, in my own words.
// ---------------------------------------------------------------------------
using System;
using BarangayDocumentSystem.Config;

namespace BarangayDocumentSystem.Models
{
    /// <summary>
    /// The barangay's own details: its name, the city and province it belongs
    /// to, the punong barangay and the office hours.
    ///
    /// Everything printed on a letterhead comes from here, so after an
    /// election there is exactly one place to change a name - and that place
    /// is App.config, not my code. I set Current once at startup from the
    /// settings file (see FromConfig) so no screen has to pass the profile
    /// down the whole call chain.
    /// </summary>
    public class BarangayProfile
    {
        public string BarangayName { get; set; }
        public string CityName { get; set; }
        public string ProvinceName { get; set; }
        public string PunongBarangay { get; set; }
        public string OfficeHours { get; set; }

        public BarangayProfile()
        {
            BarangayName = string.Empty;
            CityName = string.Empty;
            ProvinceName = string.Empty;
            PunongBarangay = string.Empty;
            OfficeHours = string.Empty;
        }

        /// <summary>The profile the running program is using.</summary>
        public static BarangayProfile Current { get; set; }

        /// <summary>The real barangay this system was written for. I keep it
        /// as the built-in fallback so the program still prints a sensible
        /// letterhead if a teammate loses a line from App.config.</summary>
        public static BarangayProfile MagugpoPoblacion
        {
            get
            {
                BarangayProfile profile = new BarangayProfile();
                profile.BarangayName = "Barangay Magugpo Poblacion";
                profile.CityName = "City of Tagum";
                profile.ProvinceName = "Davao del Norte";
                profile.PunongBarangay = "HON. EUGENIA SOLIS HINGPIT, MD";
                profile.OfficeHours = "Monday to Friday, 8:00 AM - 5:00 PM";
                return profile;
            }
        }

        /// <summary>Takes whatever App.config says, keeping the built-in
        /// values for anything that was left out.</summary>
        public static BarangayProfile FromConfig()
        {
            BarangayProfile fallback = MagugpoPoblacion;

            BarangayProfile profile = new BarangayProfile();
            profile.BarangayName = AppConfig.BarangayName;
            profile.CityName = AppConfig.CityName;
            profile.ProvinceName = AppConfig.ProvinceName;
            profile.PunongBarangay = AppConfig.PunongBarangay;
            profile.OfficeHours = AppConfig.OfficeHours;

            if (string.IsNullOrWhiteSpace(profile.BarangayName)) profile.BarangayName = fallback.BarangayName;
            if (string.IsNullOrWhiteSpace(profile.CityName)) profile.CityName = fallback.CityName;
            if (string.IsNullOrWhiteSpace(profile.ProvinceName)) profile.ProvinceName = fallback.ProvinceName;
            if (string.IsNullOrWhiteSpace(profile.PunongBarangay)) profile.PunongBarangay = fallback.PunongBarangay;
            if (string.IsNullOrWhiteSpace(profile.OfficeHours)) profile.OfficeHours = fallback.OfficeHours;

            return profile;
        }

        /// <summary>"Barangay Magugpo Poblacion, City of Tagum, Davao del Norte".</summary>
        public string GetLocationLine()
        {
            return BarangayName + ", " + CityName + ", " + ProvinceName;
        }

        static BarangayProfile()
        {
            Current = MagugpoPoblacion;
        }
    }
}
