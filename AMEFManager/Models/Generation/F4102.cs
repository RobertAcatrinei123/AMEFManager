using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AMEFManager.Models.Generation;

[XmlRoot(ElementName = "form1")]
public class F4102Type
{
    [XmlElement(ElementName = "Antet")]
    public AntetF4102 Antet { get; set; } = new();

    [XmlElement(ElementName = "cntFrm")]
    public CntFrmF4102 CntFrm { get; set; } = new();

    [XmlElement(ElementName = "AmefUtl")]
    public List<AmefUtlF4102> AmefUtl { get; set; } = new();

}

public class AntetF4102
{
    [XmlElement(ElementName = "IdDoc")]
    public IdDocF4102 IdDoc { get; set; } = new();

    [XmlElement(ElementName = "NumeDoc")]
    public NumeDocF4102 NumeDoc { get; set; } = new();
}

public class IdDocF4102
{
    [XmlElement(ElementName = "universalCode")]
    public string UniversalCode { get; set; } = "F4102_A1.0.7";

    [XmlElement(ElementName = "formValid")]
    public string FormValid { get; set; } = "FORMULAR NEVALIDAT";
}

public class NumeDocF4102
{
    [XmlElement(ElementName = "Header")]
    public HeaderF4102 Header { get; set; } = new();

    [XmlElement(ElementName = "totalPlata_A")]
    public long TotalPlataA { get; set; }

    [XmlElement(ElementName = "d_rec")]
    public int DRec { get; set; } = 0;

    [XmlElement(ElementName = "an_r")]
    public int AnR { get; set; }

    [XmlElement(ElementName = "luna_r")]
    public int LunaR { get; set; }
}

public class HeaderF4102
{
    [XmlElement(ElementName = "initMsg")]
    public string InitMsg { get; set; } = "0";
}

public class CntFrmF4102
{
    [XmlElement(ElementName = "cif")]
    public string Cif { get; set; } = string.Empty;

    [XmlElement(ElementName = "denDS")]
    public string DenDS { get; set; } = string.Empty;

    [XmlElement(ElementName = "checkD")]
    public int CheckD { get; set; } = 0;

    [XmlElement(ElementName = "checkS")]
    public int CheckS { get; set; } = 1;

    [XmlElement(ElementName = "emailDS")]
    public string EmailDS { get; set; } = string.Empty;

    [XmlElement(ElementName = "telefonDS")]
    public string TelefonDS { get; set; } = string.Empty;

    [XmlElement(ElementName = "prsInrg")]
    public PrsInrgF4102 PrsInrg { get; set; } = new();

    [XmlElement(ElementName = "denAdmin")]
    public string DenAdmin { get; set; } = string.Empty;
}

public class PrsInrgF4102
{
    [XmlElement(ElementName = "cltP")]
    public int CltP { get; set; } = 1;

    [XmlElement(ElementName = "denPI")]
    public string DenPI { get; set; } = string.Empty;

    [XmlElement(ElementName = "cifPI")]
    public string CifPI { get; set; } = string.Empty;

    [XmlElement(ElementName = "emailPI")]
    public string EmailPI { get; set; } = string.Empty;

    [XmlElement(ElementName = "telefonPI")]
    public string TelefonPI { get; set; } = string.Empty;

    [XmlElement(ElementName = "RB")]
    public int RB { get; set; } = 1;

    [XmlElement(ElementName = "altC")]
    public AltCF4102? AltC { get; set; }

    public bool ShouldSerializeAltC() => AltC != null && !string.IsNullOrEmpty(AltC.SpcS);
}

public class AltCF4102
{
    [XmlElement(ElementName = "spcS")]
    public string SpcS { get; set; } = string.Empty;
}

public class AmefUtlF4102
{
    [XmlElement(ElementName = "nU")]
    public int NU { get; set; }

    [XmlElement(ElementName = "cifU")]
    public string CifU { get; set; } = string.Empty;

    [XmlElement(ElementName = "denU")]
    public string DenU { get; set; } = string.Empty;

    [XmlElement(ElementName = "cntU")]
    public string CntU { get; set; } = string.Empty;

    [XmlElement(ElementName = "Data1")]
    public string Data1 { get; set; } = string.Empty;

    [XmlElement(ElementName = "Data2")]
    public string Data2 { get; set; } = string.Empty;

    [XmlElement(ElementName = "Amef")]
    public List<AmefItemF4102> Amef { get; set; } = new();

}

public class AmefItemF4102
{
    [XmlElement(ElementName = "nA")]
    public int NA { get; set; }

    [XmlElement(ElementName = "nrTs")]
    public string NrTs { get; set; } = string.Empty;

    [XmlElement(ElementName = "nrAmef")]
    public string NrAmef { get; set; } = string.Empty;

    [XmlElement(ElementName = "dataInsAmef")]
    public string DataInsAmef { get; set; } = string.Empty;

    [XmlElement(ElementName = "locInst")]
    public LocInstF4102 LocInst { get; set; } = new();

}

public class LocInstF4102
{
    [XmlElement(ElementName = "adr")]
    public AdrF4102 Adr { get; set; } = new();

    [XmlElement(ElementName = "taxi")]
    public TaxiF4102? Taxi { get; set; }

    public bool ShouldSerializeTaxi() => Taxi != null && !string.IsNullOrEmpty(Taxi.NrAuto);

    [XmlElement(ElementName = "amb")]
    public int Amb { get; set; } = 0;

    [XmlElement(ElementName = "nesupravegheat")]
    public int Nesupravegheat { get; set; } = 0;
}

public class AdrF4102
{
    [XmlElement(ElementName = "judet")]
    public int Judet { get; set; } = 13;

    [XmlElement(ElementName = "loc")]
    public string Loc { get; set; } = string.Empty;

    [XmlElement(ElementName = "str")]
    public string Str { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr")]
    public string Nr { get; set; } = string.Empty;

    [XmlElement(ElementName = "bloc")]
    public string Bloc { get; set; } = string.Empty;

    [XmlElement(ElementName = "etaj")]
    public string Etaj { get; set; } = string.Empty;

    [XmlElement(ElementName = "apt")]
    public string Apt { get; set; } = string.Empty;

    [XmlElement(ElementName = "alt")]
    public string Alt { get; set; } = string.Empty;
}

public class TaxiF4102
{
    [XmlElement(ElementName = "nrAuto")]
    public string NrAuto { get; set; } = string.Empty;
}
