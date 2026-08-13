using System;

namespace AMEFManager.Helpers;

public static class CountyHelper
{
    /// <summary>
    /// Maps a Romanian county name, abbreviation, or numeric string to an official ANAF county code (1..52).
    /// Defaults to 13 (Constanța) if missing or unparseable. NEVER returns 0 or an invalid code outside [1..52].
    /// </summary>
    public static int GetCountyCode(string? county)
    {
        if (string.IsNullOrWhiteSpace(county))
            return 13; // Default Constanta

        string trimmed = county.Trim();
        if (int.TryParse(trimmed, out int numericCode) && numericCode >= 1 && numericCode <= 52)
        {
            return numericCode;
        }

        // Normalize text: remove common prefixes and Romanian diacritics
        string normalized = trimmed.ToUpperInvariant()
            .Replace("JUD.", "")
            .Replace("JUDETUL", "")
            .Replace("JUDET", "")
            .Trim();

        normalized = normalized
            .Replace("Ă", "A")
            .Replace("Â", "A")
            .Replace("Î", "I")
            .Replace("Ș", "S")
            .Replace("Ş", "S")
            .Replace("Ț", "T")
            .Replace("Ţ", "T");

        return normalized switch
        {
            "ALBA" or "AB" => 1,
            "ARAD" or "AR" => 2,
            "ARGES" or "AG" => 3,
            "BACAU" or "BC" => 4,
            "BIHOR" or "BH" => 5,
            "BISTRITA-NASAUD" or "BISTRITA NASAUD" or "BISTRITA" or "BN" => 6,
            "BOTOSANI" or "BT" => 7,
            "BRASOV" or "BV" => 8,
            "BRAILA" or "BR" => 9,
            "BUZAU" or "BZ" => 10,
            "CARAS-SEVERIN" or "CARAS SEVERIN" or "CARAS" or "CS" => 11,
            "CALARASI" or "CL" => 12,
            "CONSTANTA" or "CT" => 13,
            "COVASNA" or "CV" => 14,
            "DAMBOVITA" or "DB" => 15,
            "DOLJ" or "DJ" => 16,
            "GALATI" or "GL" => 17,
            "GIURGIU" or "GR" => 18,
            "GORJ" or "GJ" => 19,
            "HARGHITA" or "HR" => 20,
            "HUNEDOARA" or "HD" => 21,
            "IALOMITA" or "IL" => 22,
            "IASI" or "IS" => 23,
            "ILFOV" or "IF" => 24,
            "MARAMURES" or "MM" => 25,
            "MEHEDINTI" or "MH" => 26,
            "MURES" or "MS" => 27,
            "NEAMT" or "NT" => 28,
            "OLT" or "OT" => 29,
            "PRAHOVA" or "PH" => 30,
            "SATU MARE" or "SATU-MARE" or "SM" => 31,
            "SALAJ" or "SJ" => 32,
            "SIBIU" or "SB" => 33,
            "SUCEAVA" or "SV" => 34,
            "TELEORMAN" or "TR" => 35,
            "TIMIS" or "TM" => 36,
            "TULCEA" or "TL" => 37,
            "VASLUI" or "VS" => 38,
            "VALCEA" or "VL" => 39,
            "VRANCEA" or "VN" => 40,
            "SECTOR 1" or "SECTOR1" or "BUCURESTI SECTOR 1" or "SECTORUL 1" => 41,
            "SECTOR 2" or "SECTOR2" or "BUCURESTI SECTOR 2" or "SECTORUL 2" => 42,
            "SECTOR 3" or "SECTOR3" or "BUCURESTI SECTOR 3" or "SECTORUL 3" => 43,
            "SECTOR 4" or "SECTOR4" or "BUCURESTI SECTOR 4" or "SECTORUL 4" => 44,
            "SECTOR 5" or "SECTOR5" or "BUCURESTI SECTOR 5" or "SECTORUL 5" => 45,
            "SECTOR 6" or "SECTOR6" or "BUCURESTI SECTOR 6" or "SECTORUL 6" => 46,
            "BUCURESTI" or "B" or "MUNICIPIUL BUCURESTI" => 51,
            "DIASPORA" or "STRAINATATE" => 52,
            _ => 13 // Default Constanta - NEVER 0
        };
    }
}
