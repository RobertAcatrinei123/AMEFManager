using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using AMEFManager.Helpers;
using iTextSharp.text.pdf;

namespace AMEFManager.Services;

public partial class DocumentGenerationService
{
    private static readonly HashSet<string> RepeatingElementNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AMEF", "amef", "Amef", "AmefUtl", "utl", "Utl",
        "AmefItem", "C802AMEF", "subform_utl", "subform_amef",
        "tabel_utl", "tabel_amef", "comments"
    };

    public virtual Task<string> GeneratePdfAsync(string xmlPath, string templatePdfPath, string outputPdfPath)
    {
        return Task.Run(() =>
        {
            var stopwatch = Stopwatch.StartNew();
            AppLogger.Log($"[GeneratePdfAsync] Started for Template: {templatePdfPath}, XML: {xmlPath}, Output: {outputPdfPath}");

            try
            {
                if (!File.Exists(templatePdfPath))
                    throw new FileNotFoundException($"Template PDF not found at path: {templatePdfPath}", templatePdfPath);

                if (!File.Exists(xmlPath))
                    throw new FileNotFoundException($"XML data file not found at path: {xmlPath}", xmlPath);

                string? outputDir = Path.GetDirectoryName(outputPdfPath);
                if (!string.IsNullOrEmpty(outputDir) && !Directory.Exists(outputDir))
                {
                    Directory.CreateDirectory(outputDir);
                }

                XmlDocument dataDoc = new XmlDocument();
                dataDoc.Load(xmlPath);

                if (dataDoc.DocumentElement == null)
                    throw new InvalidDataException($"XML document at '{xmlPath}' does not contain a root element.");

                var root = dataDoc.DocumentElement;

                using (FileStream templateStream = new FileStream(templatePdfPath, FileMode.Open, FileAccess.Read))
                using (FileStream outputStream = new FileStream(outputPdfPath, FileMode.Create, FileAccess.Write))
                {
                    PdfReader reader = new PdfReader(templateStream);

                    // 1. DO NOT strip /Perms or call RemoveUsageRights().
                    //    Append mode ('\0', true) preserves the Adobe Reader Extensions certificate.
                    PdfStamper stamper = new PdfStamper(reader, outputStream, '\0', true);

                    var xfa = stamper.AcroFields.Xfa;
                    XmlDocument? xfaDom = xfa.DomDocument;
                    XmlNode? datasetsNode = xfa.DatasetsNode;

                    if (xfaDom == null)
                        throw new InvalidOperationException("PDF template does not contain a valid XFA DomDocument.");

                    if (datasetsNode == null)
                    {
                        XmlNodeList nodes = xfaDom.GetElementsByTagName("xfa:datasets");
                        if (nodes.Count > 0) datasetsNode = nodes[0]!;
                    }

                    if (datasetsNode == null)
                        throw new InvalidOperationException("DatasetsNode is null. Unable to locate XFA dataset node.");

                    // 2. Locate or create <xfa:data>
                    XmlNode? dataNode = null;
                    foreach (XmlNode child in datasetsNode.ChildNodes)
                    {
                        if (child.Name == "xfa:data" || child.LocalName == "data")
                        {
                            dataNode = child;
                            break;
                        }
                    }

                    if (dataNode == null)
                    {
                        var newElem = xfaDom.CreateElement("xfa:data", "http://www.xfa.org/schema/xfa-data/1.0/");
                        var dataNodeAttr = xfaDom.CreateAttribute("xfa", "dataNode", "http://www.xfa.org/schema/xfa-data/1.0/");
                        dataNodeAttr.Value = "dataGroup";
                        newElem.SetAttributeNode(dataNodeAttr);
                        datasetsNode.AppendChild(newElem);
                        dataNode = newElem;
                    }
                    else
                    {
                        if (dataNode is XmlElement existingDataElem && string.IsNullOrEmpty(existingDataElem.GetAttribute("xfa:dataNode")))
                        {
                            var dataNodeAttr = xfaDom.CreateAttribute("xfa", "dataNode", "http://www.xfa.org/schema/xfa-data/1.0/");
                            dataNodeAttr.Value = "dataGroup";
                            existingDataElem.SetAttributeNode(dataNodeAttr);
                        }
                    }

                    // 3. Inject XML nodes into <xfa:data>
                    MergeData(dataNode, root, xfaDom);

                    // 4. Serialize the updated datasets node
                    byte[] datasetBytes = Encoding.UTF8.GetBytes(datasetsNode.OuterXml);
                    PdfStream newDatasetsStream = new PdfStream(datasetBytes);

                    // 5. Locate the indirect reference for 'datasets' inside the XFA packet array
                    PdfDictionary catalog = reader.Catalog;
                    PdfDictionary acroForm = catalog.GetAsDict(new PdfName("AcroForm"));
                    if (acroForm != null)
                    {
                        PdfObject xfaObj = acroForm.Get(new PdfName("XFA"));
                        if (xfaObj != null && xfaObj.IsArray())
                        {
                            PdfArray xfaArray = (PdfArray)xfaObj;
                            int datasetsRefIndex = -1;

                            for (int i = 0; i < xfaArray.Size; i += 2)
                            {
                                PdfObject keyObj = xfaArray[i];
                                if (keyObj is PdfString keyStr && string.Equals(keyStr.ToString(), "datasets", StringComparison.OrdinalIgnoreCase))
                                {
                                    datasetsRefIndex = i + 1;
                                    break;
                                }
                            }

                            if (datasetsRefIndex != -1)
                            {
                                PdfObject targetObj = xfaArray[datasetsRefIndex];
                                if (targetObj is PdfIndirectReference indRef)
                                {
                                    // Replace the indirect stream object in the incremental revision
                                    stamper.Writer.AddToBody(newDatasetsStream, indRef.Number);
                                }
                            }
                        }
                    }

                    // 6. Keep Changed = false so iTextSharp does not collapse the 16-packet array
                    stamper.AcroFields.Xfa.Changed = false;

                    stamper.Close();
                    reader.Close();
                }

                stopwatch.Stop();
                AppLogger.Log($"[GeneratePdfAsync] Successfully completed PDF generation in {stopwatch.ElapsedMilliseconds}ms.");
                return outputPdfPath;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                AppLogger.Log($"[GeneratePdfAsync] EXCEPTION: {ex.Message}\n{ex.StackTrace}");
                AppLogger.LogException(ex, "DocumentGenerationService.GeneratePdfAsync");
                throw;
            }
        });
    }

    private void MergeData(XmlNode targetDataNode, XmlNode sourceRoot, XmlDocument ownerDoc)
    {
        if (sourceRoot == null) return;

        XmlNode? targetRoot = null;
        int elementChildrenCount = 0;
        foreach (XmlNode child in targetDataNode.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                elementChildrenCount++;
                if (string.Equals(child.LocalName, sourceRoot.LocalName, StringComparison.OrdinalIgnoreCase))
                {
                    targetRoot = child;
                }
            }
        }

        if (targetRoot != null)
        {
            RecursiveMergeNodes(targetRoot, sourceRoot, ownerDoc);
            return;
        }

        if (elementChildrenCount == 0)
        {
            while (targetDataNode.HasChildNodes)
            {
                targetDataNode.RemoveChild(targetDataNode.FirstChild!);
            }

            if (targetDataNode is XmlElement targetElem && !targetElem.HasAttribute("dataNode", "http://www.xfa.org/schema/xfa-data/1.0/") && !targetElem.HasAttribute("xfa:dataNode") && !targetElem.HasAttribute("dataNode"))
            {
                var dataNodeAttr = ownerDoc.CreateAttribute("xfa", "dataNode", "http://www.xfa.org/schema/xfa-data/1.0/");
                dataNodeAttr.Value = "dataGroup";
                targetElem.SetAttributeNode(dataNodeAttr);
            }

            var clonedNode = CloneElementWithoutNamespace(sourceRoot, ownerDoc);
            targetDataNode.AppendChild(clonedNode);
            return;
        }

        var sourceChildrenGrouped = new Dictionary<string, List<XmlNode>>(StringComparer.OrdinalIgnoreCase);
        foreach (XmlNode child in sourceRoot.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                if (!sourceChildrenGrouped.ContainsKey(child.LocalName))
                    sourceChildrenGrouped[child.LocalName] = new List<XmlNode>();
                sourceChildrenGrouped[child.LocalName].Add(child);
            }
        }

        XmlNode defaultAppendParent = targetDataNode;
        foreach (XmlNode child in targetDataNode.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                defaultAppendParent = child;
                break;
            }
        }

        foreach (var kvp in sourceChildrenGrouped)
        {
            string originalNodeName = kvp.Key;
            var sourceNodes = kvp.Value;
            bool isRepeating = IsRepeatingElementName(originalNodeName) || sourceNodes.Count > 1;

            var existingNodesInTarget = targetDataNode.SelectNodes($".//*[local-name()='{originalNodeName}']");

            if ((existingNodesInTarget == null || existingNodesInTarget.Count == 0) && string.Equals(originalNodeName, "an", StringComparison.OrdinalIgnoreCase))
                existingNodesInTarget = targetDataNode.SelectNodes(".//*[local-name()='an_r']");
            if ((existingNodesInTarget == null || existingNodesInTarget.Count == 0) && string.Equals(originalNodeName, "luna", StringComparison.OrdinalIgnoreCase))
                existingNodesInTarget = targetDataNode.SelectNodes(".//*[local-name()='luna_r']");
            if ((existingNodesInTarget == null || existingNodesInTarget.Count == 0) && string.Equals(originalNodeName, "den_solicintant", StringComparison.OrdinalIgnoreCase))
                existingNodesInTarget = targetDataNode.SelectNodes(".//*[local-name()='den_solicitant']");

            if (existingNodesInTarget != null && existingNodesInTarget.Count > 0)
            {
                XmlNode targetMatched = existingNodesInTarget[0]!;
                XmlNode parentNode = targetMatched.ParentNode ?? targetDataNode;
                string matchedName = targetMatched.LocalName;

                if (isRepeating)
                {
                    var clonedFirst = CloneElementWithoutNamespace(sourceNodes[0], ownerDoc, matchedName);
                    parentNode.ReplaceChild(clonedFirst, targetMatched);
                    XmlNode prevNode = clonedFirst;

                    for (int i = 1; i < sourceNodes.Count; i++)
                    {
                        var nextCloned = CloneElementWithoutNamespace(sourceNodes[i], ownerDoc, matchedName);
                        parentNode.InsertAfter(nextCloned, prevNode);
                        prevNode = nextCloned;
                    }

                    for (int k = 1; k < existingNodesInTarget.Count; k++)
                    {
                        var extraNode = existingNodesInTarget[k]!;
                        if (extraNode.ParentNode != null)
                        {
                            extraNode.ParentNode.RemoveChild(extraNode);
                        }
                    }
                }
                else
                {
                    XmlNode sNode = sourceNodes[0];
                    bool sHasElements = false;
                    foreach (XmlNode sc in sNode.ChildNodes)
                    {
                        if (sc.NodeType == XmlNodeType.Element)
                        {
                            sHasElements = true;
                            break;
                        }
                    }

                    if (sHasElements)
                    {
                        RecursiveMergeNodes(targetMatched, sNode, ownerDoc);
                    }
                    else
                    {
                        targetMatched.InnerText = sNode.InnerText;
                        MergeAttributes(targetMatched, sNode, ownerDoc);
                    }
                }
            }
            else
            {
                foreach (var sNode in sourceNodes)
                {
                    var clonedNode = CloneElementWithoutNamespace(sNode, ownerDoc);
                    defaultAppendParent.AppendChild(clonedNode);
                }
            }
        }
    }

    private void RecursiveMergeNodes(XmlNode targetNode, XmlNode sourceNode, XmlDocument ownerDoc)
    {
        if (targetNode == null || sourceNode == null) return;

        MergeAttributes(targetNode, sourceNode, ownerDoc);

        bool sourceHasChildElements = false;
        foreach (XmlNode child in sourceNode.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                sourceHasChildElements = true;
                break;
            }
        }

        if (!sourceHasChildElements)
        {
            if (!string.IsNullOrEmpty(sourceNode.InnerText) || targetNode.ChildNodes.Count <= 1)
            {
                targetNode.InnerText = sourceNode.InnerText;
            }
            return;
        }

        var sourceChildrenGrouped = new Dictionary<string, List<XmlNode>>(StringComparer.OrdinalIgnoreCase);
        foreach (XmlNode child in sourceNode.ChildNodes)
        {
            if (child.NodeType == XmlNodeType.Element)
            {
                if (!sourceChildrenGrouped.ContainsKey(child.LocalName))
                    sourceChildrenGrouped[child.LocalName] = new List<XmlNode>();
                sourceChildrenGrouped[child.LocalName].Add(child);
            }
        }

        foreach (var kvp in sourceChildrenGrouped)
        {
            string groupName = kvp.Key;
            var sourceList = kvp.Value;
            bool isRepeating = IsRepeatingElementName(groupName) || sourceList.Count > 1;

            var matchingTargetChildren = new List<XmlNode>();
            foreach (XmlNode tChild in targetNode.ChildNodes)
            {
                if (tChild.NodeType == XmlNodeType.Element &&
                    string.Equals(tChild.LocalName, groupName, StringComparison.OrdinalIgnoreCase))
                {
                    matchingTargetChildren.Add(tChild);
                }
            }

            if (isRepeating)
            {
                if (matchingTargetChildren.Count > 0)
                {
                    var firstCloned = CloneElementWithoutNamespace(sourceList[0], ownerDoc, matchingTargetChildren[0].LocalName);
                    targetNode.ReplaceChild(firstCloned, matchingTargetChildren[0]);
                    XmlNode prevNode = firstCloned;

                    for (int i = 1; i < sourceList.Count; i++)
                    {
                        var nextCloned = CloneElementWithoutNamespace(sourceList[i], ownerDoc, matchingTargetChildren[0].LocalName);
                        targetNode.InsertAfter(nextCloned, prevNode);
                        prevNode = nextCloned;
                    }

                    for (int k = 1; k < matchingTargetChildren.Count; k++)
                    {
                        targetNode.RemoveChild(matchingTargetChildren[k]);
                    }
                }
                else
                {
                    foreach (var sItem in sourceList)
                    {
                        var cloned = CloneElementWithoutNamespace(sItem, ownerDoc);
                        targetNode.AppendChild(cloned);
                    }
                }
            }
            else
            {
                XmlNode sNode = sourceList[0];
                if (matchingTargetChildren.Count > 0)
                {
                    XmlNode tChild = matchingTargetChildren[0];
                    bool sHasElements = false;
                    foreach (XmlNode sc in sNode.ChildNodes)
                    {
                        if (sc.NodeType == XmlNodeType.Element)
                        {
                            sHasElements = true;
                            break;
                        }
                    }

                    if (sHasElements)
                    {
                        RecursiveMergeNodes(tChild, sNode, ownerDoc);
                    }
                    else
                    {
                        tChild.InnerText = sNode.InnerText;
                        MergeAttributes(tChild, sNode, ownerDoc);
                    }
                }
                else
                {
                    var cloned = CloneElementWithoutNamespace(sNode, ownerDoc);
                    targetNode.AppendChild(cloned);
                }
            }
        }
    }

    private void MergeAttributes(XmlNode targetNode, XmlNode sourceNode, XmlDocument ownerDoc)
    {
        if (sourceNode.Attributes == null || targetNode.Attributes == null) return;

        foreach (XmlAttribute attr in sourceNode.Attributes)
        {
            if (IsXmlnsAttribute(attr)) continue;

            var existingAttr = targetNode.Attributes[attr.LocalName];
            if (existingAttr != null)
            {
                existingAttr.Value = attr.Value;
            }
            else
            {
                var newAttr = ownerDoc.CreateAttribute(attr.LocalName);
                newAttr.Value = attr.Value;
                targetNode.Attributes.SetNamedItem(newAttr);
            }
        }
    }

    private static bool IsXmlnsAttribute(XmlAttribute attr)
    {
        return attr.Prefix == "xmlns"
            || attr.Name == "xmlns"
            || attr.Name.StartsWith("xmlns:", StringComparison.OrdinalIgnoreCase)
            || attr.LocalName == "xmlns"
            || attr.NamespaceURI == "http://www.w3.org/2000/xmlns/";
    }

    private bool IsRepeatingElementName(string name)
    {
        return RepeatingElementNames.Contains(name);
    }

    private XmlElement CloneElementWithoutNamespace(XmlNode sourceNode, XmlDocument targetDoc, string? overrideName = null)
    {
        string elementName = !string.IsNullOrEmpty(overrideName) ? overrideName : sourceNode.LocalName;
        XmlElement newElement = targetDoc.CreateElement(elementName);

        if (sourceNode.Attributes != null)
        {
            foreach (XmlAttribute attr in sourceNode.Attributes)
            {
                if (IsXmlnsAttribute(attr)) continue;

                XmlAttribute newAttr = targetDoc.CreateAttribute(attr.LocalName);
                newAttr.Value = attr.Value;
                newElement.Attributes.Append(newAttr);
            }
        }

        foreach (XmlNode child in sourceNode.ChildNodes)
        {
            XmlNode? clonedChild = CloneNodeWithoutNamespace(child, targetDoc);
            if (clonedChild != null)
            {
                newElement.AppendChild(clonedChild);
            }
        }

        return newElement;
    }

    private XmlNode? CloneNodeWithoutNamespace(XmlNode sourceNode, XmlDocument targetDoc)
    {
        if (sourceNode == null) return null;

        switch (sourceNode.NodeType)
        {
            case XmlNodeType.Element:
                return CloneElementWithoutNamespace(sourceNode, targetDoc);
            case XmlNodeType.Text:
                return targetDoc.CreateTextNode(sourceNode.Value ?? string.Empty);
            case XmlNodeType.CDATA:
                return targetDoc.CreateCDataSection(sourceNode.Value ?? string.Empty);
            case XmlNodeType.Comment:
                return targetDoc.CreateComment(sourceNode.Value ?? string.Empty);
            case XmlNodeType.Whitespace:
            case XmlNodeType.SignificantWhitespace:
                return targetDoc.CreateWhitespace(sourceNode.Value ?? string.Empty);
            default:
                return null;
        }
    }
}
