using System.Xml.Serialization;

namespace Core.Models
{
    /// <summary>
    /// Ответ, содержащий строки с совпадающим хэшом
    /// </summary>
    [XmlRoot(Namespace = "http://ccfit.nsu.ru/schema/crack-hash-response",
             ElementName = "CrackHashWorkerResponse")]
    public class CrackHashWorkerResponse
    {
        /// <summary>
        /// GUID запроса
        /// </summary>
        [XmlElement("RequestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Номер запроса
        /// </summary>
        [XmlElement("PartNumber")]
        public int PartNumber { get; set; }

        /// <summary>
        /// Строки с совпадающим хэшом
        /// </summary>
        [XmlElement("Answers")]
        public Answers Answers { get; set; }
    }

    /// <summary>
    /// Строки с совпадающим хэшом
    /// </summary>
    public class Answers
    {
        [XmlElement("words")]
        public string[] Words { get; set; }
    }
}
