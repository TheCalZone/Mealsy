using Newtonsoft.Json;
using OpenAI_API;
using OpenAI_API.Chat;
using OpenAI_API.Completions;
using OpenAI_API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace RecipeApp.Services
{
    public class OpenAIApiService
    {
        private const string ApiUrl = "https://api.openai.com/v1/chat/completions";
        private const string ApiKey = "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";


        OpenAIAPI api = new OpenAIAPI("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");

        public async Task<string> AskGpt(string input)
        {
            var result = await api.Chat.CreateChatCompletionAsync(new ChatRequest()
            {
                Model = Model.GPT4_Turbo,
                MaxTokens = 300,
                Messages = new ChatMessage[] {
            new ChatMessage(ChatMessageRole.User, input)
        }
            });

            return result.ToString();
        }

        //public async Task<string> AskGpt4(string input)
        //{
        //    var httpClient = new HttpClient();

        //    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ApiKey);

        //    var content = new StringContent(
        //        JsonConvert.SerializeObject(new { prompt = input, max_tokens = 60 }),
        //        Encoding.UTF8,
        //        "application/json");

        //    var response = await httpClient.PostAsync(ApiUrl, content);

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        throw new Exception($"API request failed with status code {response.StatusCode}");
        //    }

        //    var responseContent = await response.Content.ReadAsStringAsync();

        //    var responseObject = JsonConvert.DeserializeObject<OpenAIResponse>(responseContent);

        //    return responseObject.Choices.FirstOrDefault()?.Text.Trim();
        //}

        //private class OpenAIResponse
        //{
        //    public List<Choice> Choices { get; set; }
        //}

        //private class Choice
        //{
        //    public string Text { get; set; }
        //}
    }

}
