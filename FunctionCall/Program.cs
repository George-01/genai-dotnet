using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;


internal class Program
{
    private static async Task Main(string[] args)
    {
        // get credentisl from user secrets
        IConfiguration config = new ConfigurationBuilder().AddUserSecrets<Program>()
    .Build();

        var credential = new ApiKeyCredential(config["GitHubModels:Token"]
            ?? throw new InvalidOperationException("Missing configuration: GitHubModels:Token"));
        var options = new OpenAIClientOptions()
        {
            Endpoint = new Uri("https://models.github.ai/inference")
        };

        // create a chat client
        IChatClient client =
            new ChatClientBuilder(new OpenAIClient(credential, options).GetChatClient("openai/gpt-5-mini").AsIChatClient())
            .UseFunctionInvocation()
            .Build();

        var chatOptions = new ChatOptions
        {
            Tools = [AIFunctionFactory.Create((string location, string unit) =>
            {
                // Make a call to a weather API to get the weather for the location
                var temperature = Random.Shared.Next(5, 20);
                var conditions = Random.Shared.Next(0, 2) == 0 ? "sunny" : "rainy";

                return $"The weather is {temperature} degrees C and {conditions}.";
            },
            "get_current_weather",
            "Get the current weather in a given location")]
        };

        List<ChatMessage> chatHistory = [new(ChatRole.System, """
            You are a hiking enthusiast who helps people discover fun hikes in their area.
            You are upbeat and friendly.
            """)];

        // Weather conversation relevant to the registered function.
        chatHistory.Add(new(ChatRole.User, """
            I live in UK and I'm looking for a moderate intensity hike. 
            What's the weather like in Rochdale, UK?
            """));

        Console.WriteLine($"{chatHistory.Last().Role} >>> {chatHistory.Last()}");

        ChatResponse response = await client.GetResponseAsync(chatHistory, chatOptions);

        chatHistory.Add(new(ChatRole.Assistant, response.Text));

        Console.WriteLine($"{chatHistory.Last().Role} >>> {chatHistory.Last()}");

    }
}