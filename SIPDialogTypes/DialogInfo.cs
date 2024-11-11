using System;
using System.Xml.Serialization;

namespace BigTyre.Phones
{
    [XmlRoot("dialog-info")]
    public class DialogInfo
    {
        [XmlAttribute("entity")]
        public string EntitySIPUri { get; set; }
        [XmlElement("dialog")]
        public Dialog Dialog { get; set; }
    }
}
