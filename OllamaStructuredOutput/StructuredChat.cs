using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models.Chat;

namespace OllamaStructuredOutput
{
    public class StructuredChat
    {
        private readonly OllamaApiClient _ollama;
        private readonly string _model;
        private readonly ILogger<StructuredChat>? _logger;

        public StructuredChat(string ollamaUrl, string model, ILogger<StructuredChat>? logger = null)
        {
            _ollama = new OllamaApiClient(new Uri(ollamaUrl));
            _model = model;
            _logger = logger;
        }
        public async Task<bool> Boolean(string question)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));

            _logger?.LogDebug("Processing boolean question: {Question}", question);

            var schema = new
            {
                type = "object",
                properties = new
                {
                    answer = new { type = "boolean" }
                },
                required = new[] { "answer" }
            };

            var response = await SendRequest<BooleanResponse>(question, schema);

            _logger?.LogDebug("Boolean response: {Answer}", response?.Answer ?? false);
            return response?.Answer ?? false;
        }

        public async Task<string?> SingleChoice(string question, string[] options)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Length == 0)
                throw new ArgumentException("Options array cannot be empty.", nameof(options));

            if (options.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Options cannot contain null or empty values.", nameof(options));

            _logger?.LogDebug("Processing single choice question: {Question} with options: {Options}",
                question, string.Join(", ", options));

            var schema = new
            {
                type = "object",
                properties = new
                {
                    selected = new
                    {
                        type = "string",
                        @enum = options
                    }
                },
                required = new[] { "selected" }
            };

            var response = await SendRequest<SingleChoiceResponse>(question, schema);

            _logger?.LogDebug("Single choice response: {Selected}", response?.Selected);
            return response?.Selected;
        }

        public async Task<List<string>> MultiChoices(string question, string[] options)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));

            if (options == null)
                throw new ArgumentNullException(nameof(options));

            if (options.Length == 0)
                throw new ArgumentException("Options array cannot be empty.", nameof(options));

            if (options.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Options cannot contain null or empty values.", nameof(options));

            _logger?.LogDebug("Processing multi-choice question: {Question} with options: {Options}",
                question, string.Join(", ", options));

            var schema = new
            {
                type = "object",
                properties = new
                {
                    selected = new
                    {
                        type = "array",
                        items = new
                        {
                            type = "string",
                            @enum = options.ToArray()
                        }
                    }
                },
                required = new[] { "selected" }
            };

            var response = await SendRequest<MultiChoiceResponse>(question, schema);
            var result = response?.Selected ?? new List<string>();

            _logger?.LogDebug("Multi-choice response: {Selected}", string.Join(", ", result));
            return result;
        }

        public async Task<string?> RegexPattern(string question, string pattern, string? description = null)
        {
            if (string.IsNullOrWhiteSpace(question))
                throw new ArgumentException("Question cannot be null or empty.", nameof(question));

            if (string.IsNullOrWhiteSpace(pattern))
                throw new ArgumentException("Pattern cannot be null or empty.", nameof(pattern));

            _logger?.LogDebug("Processing regex pattern question: {Question} with pattern: {Pattern}",
                question, pattern);

            var schema = new
            {
                type = "object",
                properties = new
                {
                    answer = new
                    {
                        type = "string",
                        description = description ?? $"Must match the regex pattern: {pattern}"
                    }
                },
                required = new[] { "answer" }
            };

            var enhancedQuestion = $"{question}\n\nIMPORTANT: Your response must match this exact regex pattern: {pattern}";
            if (!string.IsNullOrEmpty(description))
            {
                enhancedQuestion += $"\nDescription: {description}";
            }

            var response = await SendRequest<RegexResponse>(enhancedQuestion, schema);

            if (!string.IsNullOrEmpty(response?.Answer) && !Regex.IsMatch(response.Answer, pattern))
            {
                _logger?.LogWarning("Response '{Answer}' doesn't match pattern '{Pattern}'",
                    response.Answer, pattern);

                return null;
            }

            _logger?.LogDebug("Regex pattern response: {Answer}", response?.Answer);
            return response?.Answer;
        }

        private async Task<T?> SendRequest<T>(string question, object schema) where T : class
        {
            try
            {
                _logger?.LogTrace("Sending request with schema: {Schema}", JsonSerializer.Serialize(schema));

                var schemaNode = JsonSerializer.SerializeToNode(schema);

                var chatRequest = new ChatRequest
                {
                    Model = _model,
                    Messages = new List<Message>
                    {
                        new() {
                            Role = ChatRole.User,
                            Content = question
                        }
                    },
                    Format = schemaNode,
                    Stream = false
                };


                var output = await _ollama.ChatAsync(chatRequest).GetContentAsync();

                _logger?.LogTrace("Raw response: {Response}", output);

                var result = JsonSerializer.Deserialize<T>(output);
                _logger?.LogDebug("Successfully deserialized response of type {Type}", typeof(T).Name);

                return result;
            }
            catch (JsonException ex)
            {
                _logger?.LogError(ex, "JSON deserialization error in SendRequest for type {Type}", typeof(T).Name);

                return null;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in SendRequest for type {Type}", typeof(T).Name);

                return null;
            }
        }
    }
}
