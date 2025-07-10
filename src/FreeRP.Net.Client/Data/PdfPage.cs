using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace FreeRP.Net.Client.Data
{
    /// <summary>
    /// PDF page as image with text
    /// </summary>
    public class PdfPage
    {
        /// <summary>
        /// Images as Data URL
        /// </summary>
        [JsonPropertyName("image")]
        public string Image { get; set; } = string.Empty;

        /// <summary>
        /// Image width
        /// </summary>
        [JsonPropertyName("width")]
        public int Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        [JsonPropertyName("height")]
        public int Height { get; set; }

        /// <summary>
        /// Text lines
        /// </summary>
        public PdfPageText[]? Texts { get; set; }
    }

    public class PdfPageText
    {
        /// <summary>
        /// Image width
        /// </summary>
        [JsonPropertyName("width")]
        public uint Width { get; set; }

        /// <summary>
        /// Image height
        /// </summary>
        [JsonPropertyName("height")]
        public uint Height { get; set; }

        /// <summary>
        /// X position from left
        /// </summary>
        [JsonPropertyName("x")]
        public uint X { get; set; }

        /// <summary>
        /// Y position form top
        /// </summary>
        [JsonPropertyName("y")]
        public uint Y { get; set; }

        /// <summary>
        /// The text
        /// </summary>
        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;
    }
}
