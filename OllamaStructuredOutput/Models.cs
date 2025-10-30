using System.Text.Json.Serialization;

namespace OllamaStructuredOutput
{
    public class BooleanResponse
    {
        [JsonPropertyName("answer")]
        public bool Answer { get; set; }
    }

    public class SingleChoiceResponse
    {
        [JsonPropertyName("selected")]
        public string Selected { get; set; } = string.Empty;
    }

    public class MultiChoiceResponse
    {
        [JsonPropertyName("selected")]
        public List<string> Selected { get; set; } = new();
    }

    public class RegexResponse
    {
        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }
}
