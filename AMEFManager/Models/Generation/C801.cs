using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AMEFManager.Models.Generation;

[XmlRoot(ElementName = "C801")]
public class C801Type
{
    [XmlElement(ElementName = "AMEF")]
    public List<AMEF> AMEF { get; set; } = new();

    [XmlElement(ElementName = "an_r")]
    public int AnR { get; set; }

    [XmlElement(ElementName = "luna_r")]
    public int LunaR { get; set; }

    [XmlElement(ElementName = "totalPlata_A")]
    public long TotalPlataA { get; set; }

    [XmlElement(ElementName = "cif")]
    public string Cif { get; set; } = string.Empty;

    [XmlElement(ElementName = "den_soc")]
    public string DenSoc { get; set; } = string.Empty;

    [XmlElement(ElementName = "adresa_soc")]
    public string AdresaSoc { get; set; } = string.Empty;

    [XmlElement(ElementName = "cif_rep")]
    public string CifRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "nume_rep")]
    public string NumeRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "adresa_rep")]
    public string AdresaRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "tip1")]
    public int Tip1 { get; set; } = 1;

    [XmlElement(ElementName = "tip2")]
    public int Tip2 { get; set; } = 0;

    [XmlElement(ElementName = "data_ci_rep")]
    public string DataCiRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "serie_ci_rep")]
    public string SerieCiRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr_ci_rep")]
    public string NrCiRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "eliberat_ci_rep")]
    public string EliberatCiRep { get; set; } = string.Empty;

    [XmlElement(ElementName = "calitate_rep")]
    public string CalitateRep { get; set; } = string.Empty;
}

public class AMEF
{
    [XmlElement(ElementName = "sub2")]
    public AmefSub2 Sub2 { get; set; } = new();

}

public class AmefSub2
{
    [XmlElement(ElementName = "tip")]
    public string Tip { get; set; } = string.Empty;

    [XmlElement(ElementName = "model")]
    public string Model { get; set; } = string.Empty;

    [XmlElement(ElementName = "config")]
    public string Config { get; set; } = string.Empty;

    [XmlElement(ElementName = "serie")]
    public string Serie { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr_autoriz")]
    public string NrAutoriz { get; set; } = string.Empty;

    [XmlElement(ElementName = "data_autoriz")]
    public string DataAutoriz { get; set; } = string.Empty;

    [XmlElement(ElementName = "jud")]
    public int Jud { get; set; } = 13;
    
    [XmlIgnore]
    public bool JudSpecified => TipActivitate == 3 && Jud > 0;

    [XmlElement(ElementName = "loc")]
    public string Loc { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool LocSpecified => TipActivitate == 3;

    [XmlElement(ElementName = "strada")]
    public string Strada { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool StradaSpecified => TipActivitate == 3 && !string.IsNullOrEmpty(Strada);

    [XmlElement(ElementName = "rest_adr")]
    public string RestAdr { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool RestAdrSpecified => TipActivitate == 3 && !string.IsNullOrEmpty(RestAdr);

    [XmlElement(ElementName = "tip_activitate")]
    public int TipActivitate { get; set; }

    [XmlElement(ElementName = "nr_taxi")]
    public string NrTaxi { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool NrTaxiSpecified => (TipActivitate == 2 || TipActivitate == 4);
}
