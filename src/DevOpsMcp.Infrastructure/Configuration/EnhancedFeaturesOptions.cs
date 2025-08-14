namespace DevOpsMcp.Infrastructure.Configuration;

public class EnhancedFeaturesOptions
{
    public const string SectionName = "EnhancedFeatures";
    
    public string DatabaseUrl { get; set; } = string.Empty;
    public string SupabaseUrl { get; set; } = string.Empty;
    public string SupabaseKey { get; set; } = string.Empty;
    public bool EnableVectorSearch { get; set; } = true;
    public int MaxSearchResults { get; set; } = 10;
    public int EmbeddingDimensions { get; set; } = 1536; // OpenAI embeddings
}