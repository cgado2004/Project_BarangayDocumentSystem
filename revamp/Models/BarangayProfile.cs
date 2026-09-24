namespace BarangayDocumentSystem.Models
{
    public class BarangayProfile
    {
        public string BarangayName { get; private set; }
        public string CityName { get; private set; }
        public string ProvinceName { get; private set; }
        public string PunongBarangay { get; private set; }

        public BarangayProfile(string barangayName, string cityName, string provinceName, string punongBarangay)
        {
            BarangayName = barangayName;
            CityName = cityName;
            ProvinceName = provinceName;
            PunongBarangay = punongBarangay;
        }
    }
}
