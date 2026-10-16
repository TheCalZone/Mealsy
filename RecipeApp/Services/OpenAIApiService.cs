using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Models;

namespace RecipeApp.Services
{
    public class OpenAIApiService
    {
        public async Task<string> AskGpt(string input)
        {
            // Never ship a shared API key in a mobile binary.
            var key = await SecureStorage.GetAsync("OpenAIApiKey");
            if (string.IsNullOrWhiteSpace(key))
                throw new InvalidOperationException("AI recipes require a configured OpenAI API key.");

            var api = new OpenAIAPI(key);
            var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest
            {
                Model = Model.GPT4_Turbo,
                MaxTokens = 300,
                Messages = new[] { new ChatMessage(ChatMessageRole.User, input) }
            });
            return result.ToString();
        }
    }
}
