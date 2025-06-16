using System.Net.Http.Headers;
using System.Text.Json.Serialization;
using System.Text;
using System.Text.Json;

namespace UniNotes.Services
{
    public class GitHubService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly string _gitHubToken;

        public GitHubService(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
            _gitHubToken = _configuration.GetValue<string>("GitHub:PersonalAccessToken") ?? "";
        }

        public async Task<SponsorshipData> GetSponsorshipData()
        {
            try
            {
                if (string.IsNullOrEmpty(_gitHubToken))
                {
                    return GetDefaultSponsorshipData();
                }

                // Set up headers for GraphQL request
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _gitHubToken);
                _httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("UniNotes", "1.0"));

                // GraphQL query to fetch sponsorship data
                var query = @"
                {
                  viewer {
                    login
                    sponsorshipsAsMaintainer(first: 100) {
                      totalCount
                      nodes {
                        tier {
                          monthlyPriceInDollars
                        }
                        sponsorEntity {
                          ... on User {
                            login
                            avatarUrl
                          }
                          ... on Organization {
                            login
                            avatarUrl
                          }
                        }
                        createdAt
                      }
                    }
                  }
                }";

                var content = new StringContent(
                    JsonSerializer.Serialize(new { query }),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.PostAsync("https://api.github.com/graphql", content);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var graphQLResponse = JsonSerializer.Deserialize<GraphQLModels.GraphQLResponse>(jsonResponse);

                    if (graphQLResponse?.Data?.Viewer != null)
                    {
                        var sponsorships = graphQLResponse.Data.Viewer.SponsorshipsAsMaintainer;
                        double totalAmount = 0;

                        var sponsors = new List<SponsorInfo>();
                        
                        if (sponsorships.Nodes != null)
                        {
                            foreach (var node in sponsorships.Nodes)
                            {
                                if (node.Tier != null && node.SponsorEntity != null)
                                {
                                    // Convert from dollars to euros (approximate conversion)
                                    double amountInEuros = node.Tier.MonthlyPriceInDollars * 0.92; // USD to EUR conversion
                                    totalAmount += amountInEuros;
                                    
                                    sponsors.Add(new SponsorInfo
                                    {
                                        Name = node.SponsorEntity.Login,
                                        AvatarUrl = node.SponsorEntity.AvatarUrl,
                                        Level = GetSponsorLevel(node.Tier.MonthlyPriceInDollars),
                                        Date = node.CreatedAt
                                    });
                                }
                            }
                        }
                        
                        // Cap at the target amount if needed
                        const double targetAmount = 20.0;
                        totalAmount = Math.Min(targetAmount, totalAmount);
                        
                        return new SponsorshipData
                        {
                            TotalAmount = Math.Round(totalAmount, 2),
                            SponsorCount = sponsorships.TotalCount,
                            Sponsors = sponsors.OrderByDescending(s => s.Date).Take(6).ToList()
                        };
                    }
                }
                
                // If anything fails, return default data
                return GetDefaultSponsorshipData();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching GitHub sponsorship data: {ex.Message}");
                return GetDefaultSponsorshipData();
            }
        }
        
        private string GetSponsorLevel(double monthlyAmount)
        {
            return monthlyAmount switch
            {
                < 10 => "Bronze",
                < 25 => "Silver", 
                < 100 => "Gold",
                _ => "Platinum"
            };
        }
        
        private SponsorshipData GetDefaultSponsorshipData()
        {
            return new SponsorshipData
            {
                TotalAmount = 0.00,
                SponsorCount = 1,
                Sponsors = new List<SponsorInfo>
                {
                    new()
                    {
                        Name = "daglaroglou",
                        AvatarUrl = "https://avatars.githubusercontent.com/u/70488503",
                        Level = "Platinum",
                        Date = DateTime.Now.AddDays(-120)
                    }
                }
            };
        }
    }

    public class SponsorshipData
    {
        public double TotalAmount { get; set; }
        public int SponsorCount { get; set; }
        public List<SponsorInfo> Sponsors { get; set; } = new();
    }
    
    public class SponsorInfo
    {
        public string Name { get; set; } = "";
        public string AvatarUrl { get; set; } = "";
        public string Level { get; set; } = "";
        public DateTime Date { get; set; }
    }
    
    // GraphQL response models - moved to a nested namespace for organization
    public static class GraphQLModels
    {
        public class GraphQLResponse
        {
            [JsonPropertyName("data")]
            public GraphQLData? Data { get; set; }
        }
        
        public class GraphQLData
        {
            [JsonPropertyName("viewer")]
            public Viewer? Viewer { get; set; }
        }
        
        public class Viewer
        {
            [JsonPropertyName("login")] 
            public string Login { get; set; } = "";
            
            [JsonPropertyName("sponsorshipsAsMaintainer")]
            public Sponsorships SponsorshipsAsMaintainer { get; set; } = new();
        }
        
        public class Sponsorships
        {
            [JsonPropertyName("totalCount")]
            public int TotalCount { get; set; }
            
            [JsonPropertyName("nodes")]
            public List<SponsorshipNode>? Nodes { get; set; }
        }
        
        public class SponsorshipNode
        {
            [JsonPropertyName("tier")]
            public Tier? Tier { get; set; }
            
            [JsonPropertyName("sponsorEntity")]
            public SponsorEntity? SponsorEntity { get; set; }
            
            [JsonPropertyName("createdAt")]
            public DateTime CreatedAt { get; set; }
        }
        
        public class Tier
        {
            [JsonPropertyName("monthlyPriceInDollars")]
            public double MonthlyPriceInDollars { get; set; }
        }
        
        public class SponsorEntity
        {
            [JsonPropertyName("login")]
            public string Login { get; set; } = "";
            
            [JsonPropertyName("avatarUrl")]
            public string AvatarUrl { get; set; } = "";
        }
    }
}