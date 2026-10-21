using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;

namespace Catalog.Services
{
    public class ProductAIService(
        IChatClient chatClient, 
        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
        VectorStoreCollection<ulong, ProductVector> productVectorCollection,
        CatalogDbContext dbContext
        )
    {

        public async Task<string> SupportAsync(string userQuery, CancellationToken cancellationToken = default)
        {
            var products = await dbContext.Products
                .AsNoTracking()
                .Select(p => new { p.Id, p.Name, p.Price })
                .ToListAsync(cancellationToken);

            string productCatalog = products.Count == 0
                ? "- (No products available)"
                : string.Join("\n", products.Select(products => $"- Id:{products.Id} Name:\"{products.Name}\" Price:{products.Price:C}"));

            var systemPrompt = $"""
            You are a helpful assistant for an outdoor camping products store.

            Rules:
            1. Only answer questions related to outdoor camping or the product catalog. If unrelated say exactly: "I only answer questions about outdoor camping products."
            2. Be concise and a little funny (light humor).
            3. If you don't know, reply exactly: "I don't know that."
            4. Do not store memory of the conversation.
            5. When appropriate (most user questions), end with ONE relevant product recommendation from the catalog below.
               - Pick the product that best matches the user's intent; if none clearly match, pick a random one.
               - Format the recommendation on a new final line as:
                 Recommendation: <Product Name> - <Price>
            6. Do NOT invent products not in the catalog.

            Product Catalog:
            {productCatalog}
            """;

            var chatHistory = new List<ChatMessage>
            {
                new(ChatRole.System, systemPrompt),
                new(ChatRole.User, userQuery)
            };

            var response = await chatClient.GetResponseAsync(chatHistory, cancellationToken: cancellationToken);

            return response.Text ?? "No description available.";
        }


        private async Task InitEmbeddingsAsync()
        {
            await productVectorCollection.EnsureCollectionExistsAsync();

            var products = await dbContext.Products.ToListAsync();
            foreach(var product in products)
            {
                var productInfo = $"[{product.Name}] is product that costs [{product.Price}] and is described as [{product.Description}].";

                var productVector = new ProductVector
                {
                    Id = (ulong)product.Id,
                    Name = product.Name,
                    Description = product.Description,
                    Price = (double)product.Price,
                    ImageUrl = product.ImageUrl,
                    Vector = await embeddingGenerator.GenerateVectorAsync(productInfo)
                };
                await productVectorCollection.UpsertAsync(productVector);
            }
        }

        public async Task<IEnumerable<Product>> SearchProductsAsync(string query)
        {
            //this should be replaced with a background job or cron job to initialize the embeddings when the application starts.
            if (!await productVectorCollection.CollectionExistsAsync())
            {
                await InitEmbeddingsAsync();
            }

            var quesryEmbedding = await embeddingGenerator.GenerateVectorAsync(query);

            var results = productVectorCollection.SearchAsync(quesryEmbedding, 1);

            List<Product> products = [];
            await foreach (var resultItem in results)
            {
                products.Add(new Product
                {
                    Id = (int)resultItem.Record.Id,
                    Name = resultItem.Record.Name,
                    Description = resultItem.Record.Description,
                    Price = (decimal)resultItem.Record.Price,
                    ImageUrl = resultItem.Record.ImageUrl
                });
            }

            return products;
            /*
            // Step 1: Use the IEmbeddingGenerator to turn the user's query into a vector.
            // Step 2: Use the IVectorStore to search Qdrant for the most similar product vectors.
            // Step 3: Get the IDs of the matching products from the search results.
            // Step 4: Retrieve the full product details from our main PostgreSQL database for those IDs.
            // Step 5: Return the list of products.

            throw new NotImplementedException();
            */
        }
    }
}
