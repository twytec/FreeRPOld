using Microsoft.FluentUI.AspNetCore.Components;

namespace FreeRP.Net.Client.Blazor.Data
{
    public class TextEdit
    {
        public string Label { get; set; } = string.Empty;
        public string? Text { get; set; }
        public bool Required { get; set; }
        public TextFieldType FieldType { get; set; }
    }
}
