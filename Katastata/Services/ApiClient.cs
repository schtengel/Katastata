using System.Net.Http;
using System.Net.Http.Json;
using Katastata.Contracts;
using Katastata.Models;

namespace Katastata.Services
{
    public class ApiClient
    {
        private readonly HttpClient _httpClient;

        public ApiClient(string baseUrl)
        {
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl.TrimEnd('/') + "/")
            };
        }

        public AuthResponse Register(AuthRequest request) =>
            Send<AuthResponse>(HttpMethod.Post, "api/auth/register", request);

        public AuthResponse Login(AuthRequest request) =>
            Send<AuthResponse>(HttpMethod.Post, "api/auth/login", request);

        public List<Category> GetCategories() => Get<List<Category>>("api/categories");
        public bool CategoryExists(string name) => Get<bool>($"api/categories/exists/{Uri.EscapeDataString(name)}");
        public void AddCategory(string name) => Send<object>(HttpMethod.Post, "api/categories", new Category { Name = name });
        public void DeleteCategory(int id) => _httpClient.DeleteAsync($"api/categories/{id}").GetAwaiter().GetResult().EnsureSuccessStatusCode();

        public List<Program> GetPrograms(int userId) => Get<List<Program>>($"api/programs?userId={userId}");
        public List<Program> GetProgramsByCategory(int categoryId) => Get<List<Program>>($"api/programs/by-category/{categoryId}");

        public Program EnsureProgram(ProgramSyncRequest request) => Send<Program>(HttpMethod.Post, "api/programs/ensure", request);
        public void BulkEnsurePrograms(List<ProgramSyncRequest> requests) => Send<object>(HttpMethod.Post, "api/programs/ensure/bulk", requests);
        public void UpdateProgram(Program program) => Send<object>(HttpMethod.Put, $"api/programs/{program.Id}", program);

        public List<Session> GetSessions(int userId) => Get<List<Session>>($"api/sessions/{userId}");
        public void CreateSession(SessionCreateRequest request) => Send<object>(HttpMethod.Post, "api/sessions", request);

        public List<Statistics> GetStatistics(int userId) => Get<List<Statistics>>($"api/statistics/{userId}");

        public void DeleteUser(int userId) => _httpClient.DeleteAsync($"api/users/{userId}").GetAwaiter().GetResult().EnsureSuccessStatusCode();

        private T Get<T>(string uri)
        {
            var response = _httpClient.GetAsync(uri).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            return response.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult()!;
        }

        private T Send<T>(HttpMethod method, string uri, object payload)
        {
            var request = new HttpRequestMessage(method, uri)
            {
                Content = JsonContent.Create(payload)
            };

            var response = _httpClient.Send(request).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();

            if (typeof(T) == typeof(object))
            {
                return (T)(object)new object();
            }

            return response.Content.ReadFromJsonAsync<T>().GetAwaiter().GetResult()!;
        }
    }
}
