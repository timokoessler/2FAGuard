using System.Text;
using System.Xml;

namespace Guard.Linux.Core
{
    /// <summary>
    /// The Windows app stores notes as a WPF FlowDocument in XAML format (DataFormats.Xaml).
    /// This class converts between that format and plain text, so notes stay compatible.
    /// </summary>
    internal static class NotesFormat
    {
        private const string PresentationNs =
            "http://schemas.microsoft.com/winfx/2006/xaml/presentation";

        private static readonly HashSet<string> formattingElements =
        [
            "Bold",
            "Italic",
            "Underline",
            "Hyperlink",
            "List",
            "Table",
        ];

        // Notes can come from imported files, so DTDs (entity expansion) are rejected
        internal static readonly XmlReaderSettings SafeReaderSettings = new()
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
        };

        internal static (string text, bool hasFormatting) ToPlainText(byte[] data)
        {
            string content = Encoding.UTF8.GetString(data).TrimStart('\uFEFF');
            if (!content.TrimStart().StartsWith('<'))
            {
                return (content, false);
            }

            XmlDocument doc = new() { PreserveWhitespace = true, XmlResolver = null };
            try
            {
                using StringReader stringReader = new(content);
                using XmlReader reader = XmlReader.Create(stringReader, SafeReaderSettings);
                doc.Load(reader);
            }
            catch (XmlException)
            {
                return (content, false);
            }

            StringBuilder sb = new();
            bool hasFormatting = false;
            List<string> blocks = [];
            CollectBlocks(doc.DocumentElement!, blocks, ref hasFormatting);
            sb.AppendJoin("\n", blocks);
            return (sb.ToString().TrimEnd('\n'), hasFormatting);
        }

        private static void CollectBlocks(XmlElement element, List<string> blocks, ref bool hasFormatting)
        {
            foreach (XmlNode child in element.ChildNodes)
            {
                if (child is not XmlElement childElement)
                {
                    continue;
                }
                if (formattingElements.Contains(childElement.LocalName))
                {
                    hasFormatting = true;
                }
                if (childElement.LocalName == "Paragraph")
                {
                    StringBuilder sb = new();
                    CollectInlines(childElement, sb, ref hasFormatting);
                    blocks.Add(sb.ToString());
                }
                else
                {
                    CollectBlocks(childElement, blocks, ref hasFormatting);
                }
            }
        }

        private static void CollectInlines(XmlElement element, StringBuilder sb, ref bool hasFormatting)
        {
            foreach (XmlNode child in element.ChildNodes)
            {
                switch (child)
                {
                    case XmlElement childElement when childElement.LocalName == "LineBreak":
                        sb.Append('\n');
                        break;
                    case XmlElement childElement:
                        if (
                            formattingElements.Contains(childElement.LocalName)
                            || childElement.HasAttribute("FontWeight")
                            || childElement.HasAttribute("FontStyle")
                            || childElement.HasAttribute("TextDecorations")
                            || childElement.HasAttribute("Foreground")
                        )
                        {
                            hasFormatting = true;
                        }
                        CollectInlines(childElement, sb, ref hasFormatting);
                        break;
                    case XmlText or XmlSignificantWhitespace or XmlWhitespace:
                        if (element.LocalName == "Run" || element.LocalName == "Span" || element.LocalName == "Bold" || element.LocalName == "Italic" || element.LocalName == "Underline" || element.LocalName == "Hyperlink" || element.LocalName == "Paragraph")
                        {
                            sb.Append(child.Value);
                        }
                        break;
                }
            }
        }

        internal static byte[] FromPlainText(string text)
        {
            XmlDocument doc = new();
            XmlElement section = doc.CreateElement("Section", PresentationNs);
            XmlAttribute space = doc.CreateAttribute("xml", "space", "http://www.w3.org/XML/1998/namespace");
            space.Value = "preserve";
            section.Attributes.Append(space);
            doc.AppendChild(section);

            string normalized = text.Replace("\r\n", "\n").Replace('\r', '\n');
            foreach (string line in normalized.Split('\n'))
            {
                XmlElement paragraph = doc.CreateElement("Paragraph", PresentationNs);
                if (line.Length > 0)
                {
                    XmlElement run = doc.CreateElement("Run", PresentationNs);
                    run.InnerText = line;
                    paragraph.AppendChild(run);
                }
                section.AppendChild(paragraph);
            }

            return Encoding.UTF8.GetBytes(doc.OuterXml);
        }
    }
}
