var builder = DistributedApplication.CreateBuilder(args);

// See https://learn.microsoft.com/dotnet/aspire/azure/local-provisioning#configuration
// for instructions providing configuration values

var openai = builder.AddOpenAI("openai")
    .WithEndpoint("https://openrouter.ai/api/v1");

var chat = openai.AddModel("chat", "openai/gpt-4o-mini");

var embeddings = openai.AddModel("embeddings", "openai/text-embedding-3-small");


var vectorDB = builder.AddQdrant("vectordb")
    .WithDataVolume()
    .WithLifetime(ContainerLifetime.Persistent);

var markitdown = builder.AddContainer("markitdown", "mcp/markitdown")
    .WithArgs("--http", "--host", "0.0.0.0", "--port", "3001")
    .WithHttpEndpoint(targetPort: 3001, name: "http");

var webApp = builder.AddProject<Projects.ChatApp_RAG_Qdrant_Web>("aichatweb-app");

webApp
    .WithReference(chat)
    .WithReference(embeddings)
    .WaitFor(chat)
    .WaitFor(embeddings);

webApp
    .WithReference(vectorDB)
    .WaitFor(vectorDB);
webApp
    .WithEnvironment("MARKITDOWN_MCP_URL", markitdown.GetEndpoint("http"));

builder.Build().Run();
