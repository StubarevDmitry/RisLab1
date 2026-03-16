using System.Xml.Serialization;

namespace Core.Models
{
    /// <summary>
    /// Запрос на взлом хэша в заданном пространстве строк
    /// </summary>
    [XmlRoot(Namespace = "http://ccfit.nsu.ru/schema/crack-hash-request",
             ElementName = "CrackHashManagerRequest")]
    public class CrackHashManagerRequest
    {
        /// <summary>
        /// GUID запроса
        /// </summary>
        [XmlElement("RequestId")]
        public string RequestId { get; set; }

        /// <summary>
        /// Номер части запроса
        /// </summary>
        [XmlElement("PartNumber")]
        public int PartNumber { get; set; }

        /// <summary>
        /// Общее количество частей
        /// </summary>
        [XmlElement("PartCount")]
        public int PartCount { get; set; }

        /// <summary>
        /// Хэш
        /// </summary>
        [XmlElement("Hash")]
        public string Hash { get; set; }

        /// <summary>
        /// Максимальная длина последовательности
        /// </summary>
        [XmlElement("MaxLength")]
        public int MaxLength { get; set; }

        /// <summary>
        /// Алфавит для генерации строк
        /// </summary>
        [XmlElement("Alphabet")]
        public Alphabet Alphabet { get; set; }
    }

    /// <summary>
    /// Алфавит для генерации строк
    /// </summary>
    public class Alphabet
    {
        [XmlElement("symbols")]
        public string[] Symbols { get; set; }
    }
}
