using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AMEFManager.Models.Generation;

[XmlRoot(ElementName = "c802")]
public class C802Type
{
    [XmlElement(ElementName = "amef")]
    public List<C802AMEF> Amef { get; set; } = new();

    [XmlElement(ElementName = "an")]
    public int An { get; set; }
    
    [XmlIgnore]
    public bool AnSpecified => An != 0;

    [XmlElement(ElementName = "luna")]
    public int Luna { get; set; }
    
    [XmlIgnore]
    public bool LunaSpecified => Luna != 0;

    [XmlElement(ElementName = "d_rec")]
    public int DRec { get; set; }
    
    [XmlIgnore]
    public bool DRecSpecified => DRec != 0;

    [XmlElement(ElementName = "id_solicitare")]
    public long IdSolicitare { get; set; }
    
    [XmlIgnore]
    public bool IdSolicitareSpecified => IdSolicitare != 0;

    [XmlElement(ElementName = "totalPlata_A")]
    public long TotalPlataA { get; set; }
    
    [XmlIgnore]
    public bool TotalPlataASpecified => TotalPlataA != 0;

    [XmlElement(ElementName = "cif")]
    public string Cif { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool CifSpecified => !string.IsNullOrEmpty(Cif);

    [XmlElement(ElementName = "den_solicitant")]
    public string DenSolicitant { get; set; } = string.Empty;
    
    [XmlIgnore]
    public bool DenSolicitantSpecified => !string.IsNullOrEmpty(DenSolicitant);

    [XmlElement(ElementName = "calitate_solicitant")]
    public int CalitateSolicitant { get; set; }
    
    [XmlIgnore]
    public bool CalitateSolicitantSpecified => CalitateSolicitant != 0;
}

public class C802AMEF
{
    [XmlElement(ElementName = "nui")]
    public string Nui { get; set; } = string.Empty;

    [XmlElement(ElementName = "utilizare")]
    public int Utilizare { get; set; }
    
    [XmlIgnore]
    public bool UtilizareSpecified => Utilizare != 0;

    [XmlElement(ElementName = "tip_profil")]
    public int TipProfil { get; set; }
    
    [XmlIgnore]
    public bool TipProfilSpecified => TipProfil != 0;

    [XmlElement(ElementName = "port")]
    public int Port { get; set; }
    
    [XmlIgnore]
    public bool PortSpecified => Port != 0;
}
