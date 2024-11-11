using System.Xml.Serialization;

namespace BigTyre.Phones
{
    public class DialogParticipant
    {
        [XmlElement("identity")]
        public string Identity { get; set; }
        [XmlElement("target")]
        public SIPTarget Target { get; set; }
    }
}
