using System.Xml.Serialization;

namespace BigTyre.Phones
{
    public class Dialog
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("call-id")]
        public string CallId { get; set; }

        [XmlAttribute("local-tag")]
        public string LocalTag { get; set; }

        [XmlAttribute("remote-tag")]
        public string RemoteTag { get; set; }

        [XmlAttribute("direction")]
        public string Direction { get; set; }

        [XmlElement("state")]
        public string State { get; set; }

        [XmlElement("local")]
        public DialogParticipant Local { get; set; }

        [XmlElement("remote")]
        public DialogParticipant Remote { get; set; }
    }
}
