using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using OpenAI;
using System.ClientModel;

// Get credentials from user secrets
IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

var apiKey = config["OpenRouter:ApiKey"]
    ?? throw new InvalidOperationException("Missing configuration: OpenRouter:ApiKey.");

var credential = new ApiKeyCredential(apiKey);

var options = new OpenAIClientOptions
{
    Endpoint = new Uri("https://openrouter.ai/api/v1")
};

// Create a chat client
IChatClient client = new OpenAIClient(credential, options).GetChatClient("openai/gpt-5-mini").AsIChatClient();

// user prompts
var promptDescribe = "Describe the image";
var promptAnalyze = "How many red cars are in the picture? and what other car colors are there?";

// prompts
string systemPrompt = "You are a useful assistant that describes images using a direct style.";
var userPrompt = promptAnalyze;

List<ChatMessage> messages =
[
    new ChatMessage(ChatRole.System, systemPrompt),
    new ChatMessage(ChatRole.User, userPrompt),
];

// read the image bytes, create a new image content part and add it to the messages
var imageFileName = "cars.png";
string image = Path.Combine(Directory.GetCurrentDirectory(), "images", imageFileName);

AIContent aic = new DataContent(File.ReadAllBytes(image), "image/png");
var message = new ChatMessage(ChatRole.User, [aic]);
messages.Add(message);

// send the messages to the assistant
var response = await client.GetResponseAsync(messages);
Console.WriteLine($"Prompt: {userPrompt}");
Console.WriteLine($"Image: {imageFileName}");
Console.WriteLine($"Response: {response.Messages[0]}");