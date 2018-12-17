using System.IO;
using System.Text;
using System.Xml;

namespace WebViewer
{
    public class CConfiguration
    {
        private System.Globalization.CultureInfo _culture = System.Globalization.CultureInfo.GetCultureInfoByIetfLanguageTag("en-US");

        private string _url = "http://ya.ru"; //start page
        private uint _resetDelay = 10; //seconds
        private string _exitPassword = "enigma";
        private string _theme = "sketch"; 


        public string Url { get => _url; set => _url = value; }
        public uint ResetDelay { get => _resetDelay; set => _resetDelay = value; }
        public string ExitPassword { get => _exitPassword; set => _exitPassword = value; }
        public string Theme { get => _theme; set => _theme = value; }

        private CConfiguration()
        { }


        #region READ AND WRITE XML

        public static CConfiguration Read(string fileName)
        {
            CConfiguration res = null;
            res = new CConfiguration();

            if (!File.Exists(fileName)) {
                res.Write(fileName);
            } else {
                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(fileName);
                // получим корневой элемент
                XmlElement xRoot = xDoc.DocumentElement;

                foreach (XmlNode xnode in xRoot) {
                    if (xnode.NodeType != XmlNodeType.Element) {
                        continue;
                    }

                    switch (xnode.Name.ToLowerInvariant()) {
                        case "url":
                            res._url = xnode.InnerText.Trim();
                            break;
                        case "resetdelay":
                            res._resetDelay = uint.Parse(xnode.InnerText.Trim(), System.Globalization.NumberStyles.Number);
                            break;
                        case "password":
                            res.ExitPassword = xnode.InnerText;
                            break;
                        case "theme":
                            res.Theme = xnode.InnerText;
                            break;
                    }
                }
            }
            return res;
        }

        public void Write(string fileName)
        {
            if (File.Exists(fileName)) {
                File.Delete(fileName);
            }

            XmlDocument xDoc = new XmlDocument();
            XmlDeclaration decl = xDoc.CreateXmlDeclaration("1.0", "utf-8", null);
            // создадим корневой элемент
            XmlElement xRoot = xDoc.CreateElement("configuration");

            xRoot.AppendChild(GetElement(xDoc, "Url", _url));
            xRoot.AppendChild(GetElement(xDoc, "ResetDelay", _resetDelay.ToString()));
            xRoot.AppendChild(GetElement(xDoc, "Password", _exitPassword));
            xRoot.AppendChild(GetElement(xDoc, "Theme", _theme));

            xDoc.AppendChild(decl);
            xDoc.AppendChild(xRoot);
            using (TextWriter sw = new StreamWriter(fileName, false, Encoding.UTF8)) //Set encoding
            { xDoc.Save(sw); }
        }

        private XmlElement GetElement(XmlDocument xDoc, string elementName, string value)
        {
            XmlElement elem = xDoc.CreateElement(elementName);
            elem.InnerText = value;
            return elem;
        }

        #endregion
    }
}
