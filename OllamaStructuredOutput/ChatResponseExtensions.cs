using OllamaSharp.Models.Chat;
using System.Text;

namespace OllamaStructuredOutput
{
    public static class ChatResponseExtensions
    {
        public static async Task<string> GetContentAsync(this IAsyncEnumerable<ChatResponseStream?> stream)
        {
            var responseBuilder = new StringBuilder();

            await foreach (var chunk in stream)
            {
                if (chunk is null)
                    continue;

                var content = chunk.Message.Content;

                if (string.IsNullOrEmpty(content))
                    continue;

                responseBuilder.Append(content);
                if (chunk.Done)
                    break;
            }

            return responseBuilder.ToString();
        }
    }
}
