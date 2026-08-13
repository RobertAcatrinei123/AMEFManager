using System.Collections.Generic;
using System.Xml.Serialization;

namespace AMEFManager.Models.Generation;

[XmlRoot(ElementName = "form1")]
public class F4103Type
{
    [XmlElement(ElementName = "Antet")]
    public AntetF4103 Antet { get; set; } = new();

    [XmlElement(ElementName = "infP")]
    public InfPF4103 InfP { get; set; } = new();

    [XmlElement(ElementName = "infS")]
    public InfSF4103 InfS { get; set; } = new();

    [XmlElement(ElementName = "AMEF")]
    public List<F4103AMEF> Amef { get; set; } = new();
}

public class AntetF4103
{
    [XmlElement(ElementName = "NumeDoc")]
    public NumeDocF4103 NumeDoc { get; set; } = new();
}

public class NumeDocF4103
{
    [XmlElement(ElementName = "universalCode")]
    public string UniversalCode { get; set; } = "F4103_A1.0.0";

    [XmlElement(ElementName = "an_r")]
    public int AnR { get; set; }

    [XmlElement(ElementName = "totalPlata_A")]
    public long TotalPlataA { get; set; }

    [XmlElement(ElementName = "d_rec")]
    public int DRec { get; set; } = 0;

    [XmlElement(ElementName = "d_zero")]
    public int DZero { get; set; } = 0;

    [XmlElement(ElementName = "nr_init")]
    public string NrInit { get; set; } = string.Empty;

    [XmlElement(ElementName = "luna_r")]
    public int LunaR { get; set; }
}

public class InfPF4103
{
    [XmlElement(ElementName = "calD1")]
    public int CalD1 { get; set; } = 0;

    [XmlElement(ElementName = "calD2")]
    public int CalD2 { get; set; } = 0;

    [XmlElement(ElementName = "cif")]
    public string Cif { get; set; } = string.Empty;

    [XmlElement(ElementName = "den")]
    public string Den { get; set; } = string.Empty;

    [XmlElement(ElementName = "email")]
    public string Email { get; set; } = string.Empty;

    [XmlElement(ElementName = "telefon")]
    public string Telefon { get; set; } = string.Empty;
}

public class InfSF4103
{
    [XmlElement(ElementName = "sub2")]
    public InfSSub2F4103 Sub2 { get; set; } = new();
}

public class InfSSub2F4103
{
    [XmlElement(ElementName = "cifP")]
    public string CifP { get; set; } = string.Empty;

    [XmlElement(ElementName = "denP")]
    public string DenP { get; set; } = string.Empty;

    [XmlElement(ElementName = "emailP")]
    public string EmailP { get; set; } = string.Empty;

    [XmlElement(ElementName = "telP")]
    public string TelP { get; set; } = string.Empty;

    [XmlElement(ElementName = "calP1")]
    public int CalP1 { get; set; } = 0;

    [XmlElement(ElementName = "calP2")]
    public int CalP2 { get; set; } = 0;

    [XmlElement(ElementName = "alta_calit")]
    public string AltaCalit { get; set; } = string.Empty;
}

public class F4103AMEF
{
    [XmlElement(ElementName = "sub2")]
    public F4103AmefSub2 Sub2 { get; set; } = new();
}

public class F4103AmefSub2
{
    [XmlElement(ElementName = "NUI")]
    public string Nui { get; set; } = string.Empty;

    [XmlElement(ElementName = "seriaI")]
    public string SeriaI { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr_aviz_ICI")]
    public string NrAvizIci { get; set; } = string.Empty;

    [XmlElement(ElementName = "data_aviz_ICI")]
    public string DataAvizIci { get; set; } = string.Empty;

    [XmlElement(ElementName = "sch_certif")]
    public int SchCertif { get; set; } = 0;

    [XmlElement(ElementName = "certif")]
    public string Certif { get; set; } = string.Empty;

    [XmlElement(ElementName = "stareA1")]
    public int StareA1 { get; set; } = 0;

    [XmlElement(ElementName = "stareA2")]
    public int StareA2 { get; set; } = 0;

    [XmlElement(ElementName = "stareA3")]
    public int StareA3 { get; set; } = 0;

    [XmlElement(ElementName = "stareA4")]
    public int StareA4 { get; set; } = 0;

    [XmlElement(ElementName = "stareB1")]
    public int StareB1 { get; set; } = 0;

    [XmlElement(ElementName = "stareB2")]
    public int StareB2 { get; set; } = 0;

    [XmlElement(ElementName = "stareB3")]
    public int StareB3 { get; set; } = 0;

    [XmlElement(ElementName = "stareB4")]
    public int StareB4 { get; set; } = 0;

    [XmlElement(ElementName = "fact1")]
    public int Fact1 { get; set; } = 0;

    [XmlElement(ElementName = "fact2")]
    public int Fact2 { get; set; } = 0;

    [XmlElement(ElementName = "fact3")]
    public int Fact3 { get; set; } = 0;

    [XmlElement(ElementName = "fact4")]
    public int Fact4 { get; set; } = 0;

    [XmlElement(ElementName = "fact5")]
    public int Fact5 { get; set; } = 0;

    [XmlElement(ElementName = "doc1")]
    public int Doc1 { get; set; } = 0;

    [XmlElement(ElementName = "doc2")]
    public int Doc2 { get; set; } = 0;

    [XmlElement(ElementName = "doc3")]
    public int Doc3 { get; set; } = 0;

    [XmlElement(ElementName = "doc_alte")]
    public string DocAlte { get; set; } = string.Empty;

    [XmlElement(ElementName = "seria_doc")]
    public string SeriaDoc { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr_doc")]
    public string NrDoc { get; set; } = string.Empty;

    [XmlElement(ElementName = "data_doc")]
    public string DataDoc { get; set; } = string.Empty;

    [XmlElement(ElementName = "cif_entit")]
    public string CifEntit { get; set; } = string.Empty;

    [XmlElement(ElementName = "den_entit")]
    public string DenEntit { get; set; } = string.Empty;

    [XmlElement(ElementName = "bifa_cor")]
    public int BifaCor { get; set; } = 0;

    [XmlElement(ElementName = "bifa_A1")]
    public int BifaA1 { get; set; } = 0;

    [XmlElement(ElementName = "bifa_A2")]
    public int BifaA2 { get; set; } = 0;

    [XmlElement(ElementName = "bifa_L")]
    public int BifaL { get; set; } = 0;

    [XmlElement(ElementName = "seria_F")]
    public string SeriaF { get; set; } = string.Empty;

    [XmlElement(ElementName = "nr_F")]
    public string NrF { get; set; } = string.Empty;

    [XmlElement(ElementName = "data_F")]
    public string DataF { get; set; } = string.Empty;

    [XmlElement(ElementName = "cif_F")]
    public string CifF { get; set; } = string.Empty;

    [XmlElement(ElementName = "den_F")]
    public string DenF { get; set; } = string.Empty;
}
