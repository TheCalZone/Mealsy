using OpenAI_API;
using OpenAI_API.Chat;

namespace RecipeApp.Services;

public class OpenAIApiService
{
    public async Task<string> AskGpt(string input)
    {
        var key = await SecureStorage.GetAsync("OpenAIApiKey");
        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("Save your AI service API key on this screen first.");
        var api = new OpenAIAPI(key);
        var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest
        {
            Model = "gpt-4o-mini", MaxTokens = 1200,
            Messages = new[] { new ChatMessage(ChatMessageRole.User, input) }
        });
        return result.Choices.FirstOrDefault()?.Message?.TextContent
            ?? throw new InvalidOperationException("The service returned no recipe. Please try again.");
    }
}
