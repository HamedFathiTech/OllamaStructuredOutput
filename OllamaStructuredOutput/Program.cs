namespace OllamaStructuredOutput
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var structuredChat = new StructuredChat("http://localhost:11434", "gemma3:12b");

            var isLanguage = await structuredChat.Boolean("Is BMW a cosmetic company?");
            Console.WriteLine($"Boolean result: {isLanguage}");


            var color = await structuredChat.SingleChoice(
                "What is sun's color?",
                ["Red", "Blue", "Green", "Yellow", "Purple", "Orange"]
            );
            Console.WriteLine($"SingleChoice result: {color}");


            var colors = await structuredChat.MultiChoices(
                "What are the primary colors?",
                ["Red", "Blue", "Green", "Yellow", "Purple", "Orange"]
            );
            Console.WriteLine($"MultiChoice result: {string.Join(", ", colors)}");


            var phone = await structuredChat.RegexPattern(
                "Generate a US phone number",
                @"^\(\d{3}\) \d{3}-\d{4}$"
            );
            Console.WriteLine($"Pattern result: {phone}");


            Console.ReadKey();
        }
    }
}
