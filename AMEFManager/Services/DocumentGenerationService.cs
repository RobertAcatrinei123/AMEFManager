using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AMEFManager.Models;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace AMEFManager.Services;

public partial class DocumentGenerationService : IDocumentGenerationService
{
    public DocumentGenerationService()
    {
    }

    public static string GetFrequency(string? contractType)
    {
        if (string.IsNullOrWhiteSpace(contractType))
            return "COMPLETEAZA MANUAL";

        var trimmed = contractType.Trim();
        if (string.Equals(trimmed, "lunar", StringComparison.OrdinalIgnoreCase))
            return "luna";
        if (string.Equals(trimmed, "anual", StringComparison.OrdinalIgnoreCase))
            return "an";
        if (string.Equals(trimmed, "sezonier", StringComparison.OrdinalIgnoreCase))
            return "sezon";

        return "COMPLETEAZA MANUAL";
    }

    private static void EnsureDirectoryExists(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    private static void ReplacePlaceholdersAndTable(OpenXmlElement root, Dictionary<string, string> replacements, Contract contract)
    {
        var paragraphs = root.Descendants<Paragraph>().ToList();

        foreach (var p in paragraphs)
        {
            string paraText = p.InnerText;
            if (paraText.Contains("{{table}}", StringComparison.OrdinalIgnoreCase))
            {
                var table = CreateEquipmentTable(contract, replacements.GetValueOrDefault("{{frequency}}", "luna"));
                p.Parent?.InsertAfter(table, p);
                p.Remove();
                continue;
            }

            ReplacePlaceholdersInParagraph(p, replacements);
        }
    }

    private static void ReplacePlaceholdersInElement(OpenXmlElement root, Dictionary<string, string> replacements)
    {
        var paragraphs = root.Descendants<Paragraph>().ToList();

        foreach (var p in paragraphs)
        {
            ReplacePlaceholdersInParagraph(p, replacements);
        }
    }

    private static void ReplacePlaceholdersInParagraph(Paragraph paragraph, Dictionary<string, string> replacements)
    {
        foreach (var kvp in replacements)
        {
            string placeholder = kvp.Key;
            string replacementValue = kvp.Value;

            int safetyLimit = 10;
            while (safetyLimit-- > 0)
            {
                var textList = paragraph.Descendants<Text>().ToList();
                if (textList.Count == 0) break;

                // 1. Fast path: check if any single Text element contains the placeholder
                bool replacedSingle = false;
                foreach (var t in textList)
                {
                    if (t.Text.Contains(placeholder, StringComparison.OrdinalIgnoreCase))
                    {
                        t.Text = ReplaceFirstCaseInsensitive(t.Text, placeholder, replacementValue);
                        if (t.Text.StartsWith(' ') || t.Text.EndsWith(' '))
                        {
                            t.Space = SpaceProcessingModeValues.Preserve;
                        }
                        replacedSingle = true;
                        break;
                    }
                }

                if (replacedSingle)
                {
                    continue;
                }

                // 2. Slow path: placeholder is split across multiple Text elements
                string fullText = string.Concat(textList.Select(t => t.Text));
                int matchIndex = fullText.IndexOf(placeholder, StringComparison.OrdinalIgnoreCase);
                if (matchIndex < 0)
                {
                    break;
                }

                int currentPos = 0;
                int startElemIdx = -1;
                int startOffsetInElem = -1;
                int endElemIdx = -1;
                int endOffsetInElem = -1;

                for (int i = 0; i < textList.Count; i++)
                {
                    int elemLen = textList[i].Text.Length;
                    int elemStart = currentPos;
                    int elemEnd = currentPos + elemLen;

                    if (startElemIdx == -1 && matchIndex >= elemStart && matchIndex < elemEnd)
                    {
                        startElemIdx = i;
                        startOffsetInElem = matchIndex - elemStart;
                    }

                    if (startElemIdx != -1 && (matchIndex + placeholder.Length) <= elemEnd)
                    {
                        endElemIdx = i;
                        endOffsetInElem = (matchIndex + placeholder.Length) - elemStart;
                        break;
                    }

                    currentPos += elemLen;
                }

                if (startElemIdx != -1 && endElemIdx != -1)
                {
                    if (startElemIdx == endElemIdx)
                    {
                        string orig = textList[startElemIdx].Text;
                        string newText = orig.Substring(0, startOffsetInElem) + replacementValue + orig.Substring(endOffsetInElem);
                        textList[startElemIdx].Text = newText;
                        if (newText.StartsWith(' ') || newText.EndsWith(' '))
                        {
                            textList[startElemIdx].Space = SpaceProcessingModeValues.Preserve;
                        }
                    }
                    else
                    {
                        string startOrig = textList[startElemIdx].Text;
                        string endOrig = textList[endElemIdx].Text;

                        string newStart = startOrig.Substring(0, startOffsetInElem) + replacementValue;
                        textList[startElemIdx].Text = newStart;
                        if (newStart.StartsWith(' ') || newStart.EndsWith(' '))
                        {
                            textList[startElemIdx].Space = SpaceProcessingModeValues.Preserve;
                        }

                        for (int i = startElemIdx + 1; i < endElemIdx; i++)
                        {
                            textList[i].Text = string.Empty;
                        }

                        string newEnd = endOrig.Substring(endOffsetInElem);
                        textList[endElemIdx].Text = newEnd;
                        if (newEnd.StartsWith(' ') || newEnd.EndsWith(' '))
                        {
                            textList[endElemIdx].Space = SpaceProcessingModeValues.Preserve;
                        }
                    }
                }
                else
                {
                    break;
                }
            }
        }
    }

    private static string ReplaceFirstCaseInsensitive(string input, string pattern, string replacement)
    {
        int index = input.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (index < 0) return input;
        return input.Substring(0, index) + replacement + input.Substring(index + pattern.Length);
    }

    private static Table CreateEquipmentTable(Contract contract, string frequency)
    {
        var table = new Table();

        // Table styling and borders
        var tableProperties = new TableProperties(
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct },
            new TableBorders(
                new TopBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = "000000" },
                new BottomBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = "000000" },
                new LeftBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = "000000" },
                new RightBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 6, Color = "000000" },
                new InsideHorizontalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" },
                new InsideVerticalBorder { Val = new EnumValue<BorderValues>(BorderValues.Single), Size = 4, Color = "000000" }
            )
        );
        table.AppendChild(tableProperties);

        // Header Row
        var headerRow = new TableRow();
        headerRow.Append(
            CreateTableCell("Nr. crt.", isHeader: true, widthPct: 800),
            CreateTableCell("Tipul si modelul AMEF", isHeader: true, widthPct: 1800),
            CreateTableCell("Seria de fabricatie", isHeader: true, widthPct: 1500),
            CreateTableCell("Adresa de lucru (locatia) AMEF", isHeader: true, widthPct: 2900),
            CreateTableCell($"Tarif/AMEF/{frequency}", isHeader: true, widthPct: 1000)
        );
        table.AppendChild(headerRow);

        // Data Rows
        var amefs = contract.Amefs;
        int index = 1;
        string rateStr = contract.Type != null ? $"{contract.Type.Value} LEI" : "0 LEI";

        foreach (var amef in amefs)
        {
            string amefAddress = amef.Address?.GetFullAddress() ?? contract.Client?.Address?.GetFullAddress() ?? string.Empty;
            string deviceType = amef.GetDeviceType();
            string model = amef.Model ?? amef.Authorization?.Model ?? string.Empty;
            string brand = amef.GetBrand();

            string amefTypeAndModel;
            if (!string.IsNullOrWhiteSpace(deviceType) && !string.IsNullOrWhiteSpace(model))
            {
                if (model.Contains(deviceType, StringComparison.OrdinalIgnoreCase))
                    amefTypeAndModel = model;
                else
                    amefTypeAndModel = $"{deviceType} {model}".Trim();
            }
            else if (!string.IsNullOrWhiteSpace(model))
            {
                amefTypeAndModel = model;
            }
            else if (!string.IsNullOrWhiteSpace(deviceType))
            {
                amefTypeAndModel = deviceType;
            }
            else if (!string.IsNullOrWhiteSpace(brand))
            {
                amefTypeAndModel = brand;
            }
            else
            {
                amefTypeAndModel = string.Empty;
            }

            var row = new TableRow();
            row.Append(
                CreateTableCell(index.ToString(), isHeader: false, widthPct: 800, align: JustificationValues.Center),
                CreateTableCell(amefTypeAndModel, isHeader: false, widthPct: 1800),
                CreateTableCell(amef.Series, isHeader: false, widthPct: 1500, align: JustificationValues.Center),
                CreateTableCell(amefAddress, isHeader: false, widthPct: 2900),
                CreateTableCell(rateStr, isHeader: false, widthPct: 1000, align: JustificationValues.Right)
            );
            table.AppendChild(row);
            index++;
        }

        return table;
    }

    private static TableCell CreateTableCell(string text, bool isHeader, int widthPct, JustificationValues? align = null)
    {
        var runProps = new RunProperties();
        if (isHeader)
        {
            runProps.Append(new Bold());
        }

        var run = new Run(runProps, new Text(text));
        
        var pProps = new ParagraphProperties();
        if (align.HasValue)
        {
            pProps.Append(new Justification { Val = align.Value });
        }
        else if (isHeader)
        {
            pProps.Append(new Justification { Val = JustificationValues.Center });
        }

        var paragraph = new Paragraph(pProps, run);

        var cellProps = new TableCellProperties(
            new TableCellWidth { Width = widthPct.ToString(), Type = TableWidthUnitValues.Pct },
            new TableCellMargin(
                new TopMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                new BottomMargin { Width = "100", Type = TableWidthUnitValues.Dxa },
                new LeftMargin { Width = "150", Type = TableWidthUnitValues.Dxa },
                new RightMargin { Width = "150", Type = TableWidthUnitValues.Dxa }
            )
        );

        if (isHeader)
        {
            cellProps.Append(new Shading { Val = ShadingPatternValues.Clear, Color = "auto", Fill = "F2F2F2" });
        }

        return new TableCell(cellProps, paragraph);
    }
}
