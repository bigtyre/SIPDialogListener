using System.Xml.Serialization;

namespace BigTyre.Phones
{
    public class SIPTarget
    {
        [XmlAttribute("uri")]
        public string Uri { get; set; }
    }
}
