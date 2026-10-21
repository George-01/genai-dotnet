using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using System.ClientModel;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Get credentials from user secrets
        IConfigurationRoot config = new ConfigurationBuilder().AddUserSecrets<Program>().Build();

        // Add services to the container.
        builder.AddServiceDefaults();

        builder.AddNpgsqlDbContext<CatalogDbContext>(connectionName: "catalogdb");
        builder.Services.AddScoped<ProductService>();
        builder.Services.AddScoped<ProductAIService>();

        //Add AI Chat Client
        var apiKey = config["OpenRouter:ApiKey"]
            ?? throw new InvalidOperationException("Missing configuration: OpenRouter:ApiKey.");

        var credential = new ApiKeyCredential(apiKey);

        var options = new OpenAIClientOptions
        {
            Endpoint = new Uri("https://openrouter.ai/api/v1")
        };

        // Create a chat client
        var chatClient = new OpenAIClient(credential, options).GetChatClient("openai/gpt-5-mini").AsIChatClient();

        // Create Embedding generator client
        var embeddingGenerator = new OpenAIClient(credential, options).GetEmbeddingClient("openai/text-embedding-3-small").AsIEmbeddingGenerator();

        builder.Services.AddChatClient(chatClient);
        builder.Services.AddEmbeddingGenerator(embeddingGenerator);

        //Add Vector DB Search Operations
        builder.AddQdrantClient("vectordb");
        builder.Services.AddQdrantCollection<ulong, ProductVector>("product-vectors");


        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.MapDefaultEndpoints();

        app.UseHttpsRedirection();

        app.UseMigration();

        app.MapProductEndpoints();

        app.Run();
    }
}