using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Skald.Import;

namespace Skald.Code.Editor
{
    public class SyncWithSkald
    {
        private static readonly HttpClient HttpClient = new();
        private const string BaseUrl = "https://skald.dual-daggers.com";
        private const string ApiInitiateChallengeUrl = "api/engine-challenge/initiate";
        private const string ApiProjectListUrl = "api/engine-export/projects";
        private static string ApiCheckChallengeUrl(string challengeId) => $"api/engine-challenge/{challengeId}/check";
        private static string ApiExportProjectUrl(string projectId) => $"api/engine-export/projects/{projectId}";
        private static string ApiRemoveTokenUrl => $"api/engine-token/remove";

        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(3);

        private const string DirectoryPath = "Assets/Resources/Skald";

        private const string requesterName = "Unity";

        public SyncWithSkald()
        {
            if (SyncWithSkaldState.IsLoggedIn)
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SyncWithSkaldState.Token);
            }
        }

        // Called by the custom inspector when the user clicks Login
        public async Task Login()
        {
            var initiateChallengeResponse = await InitiateChallenge(BaseUrl);
            OpenBrowser(initiateChallengeResponse.VerificationUrl);
            var checkChallengeResponse = await CheckChallenge(initiateChallengeResponse.ChallengeId, DateTime.Parse(initiateChallengeResponse.ExpiresAt), BaseUrl);
            SyncWithSkaldState.Login(checkChallengeResponse.Token);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", checkChallengeResponse.Token);
        }

        // Called by the custom inspector when the user clicks Sync
        public async Task<SkaldProject[]> GetProjects()
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/{ApiProjectListUrl}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to get projects: {responseBody}");
            }

            var projects = JsonConvert.DeserializeObject<SkaldProject[]>(responseBody);

            return projects ?? Array.Empty<SkaldProject>();
        }

        public async Task<bool> LoadProject(string projectId)
        {
            var response = await HttpClient.GetAsync($"{BaseUrl}/{ApiExportProjectUrl(projectId)}");

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to load project: {errorBody}");
            }

            Directory.CreateDirectory(DirectoryPath);

            await using var httpStream = await response.Content.ReadAsStreamAsync();
            await using var fileStream = new FileStream(
                Path.Combine(DirectoryPath, $"{projectId}.json"),
                FileMode.Create,
                FileAccess.Write
            );

            await httpStream.CopyToAsync(fileStream);

            return true;
        }

        public Task Logout()
        {
            var task = HttpClient.GetAsync($"{BaseUrl}/{ApiRemoveTokenUrl}");
            HttpClient.DefaultRequestHeaders.Authorization = null;
            SyncWithSkaldState.Logout();
            return task;
        }

        private async Task<InitiateChallengeResponse> InitiateChallenge(string baseUrl)
        {
            var body = new Dictionary<string, string> {
                {"deviceId", SyncWithSkaldState.DeviceId},
                {"requesterName", requesterName}
            };

            var response = await HttpClient.PostAsync(
                $"{baseUrl}/{ApiInitiateChallengeUrl}",
                new FormUrlEncodedContent(body)
            );
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Login failed: {(int)response.StatusCode}"
                );
            }

            var data = JsonConvert.DeserializeObject<InitiateChallengeResponse>(responseText);
            return data;
        }

        private async Task<CheckChallengeResponse> CheckChallenge(string challengeId, DateTime expiresAt, string baseUrl)
        {
            var url = $"{baseUrl}/{ApiCheckChallengeUrl(challengeId)}";

            while (new DateTime() < expiresAt)
            {
                var body = new Dictionary<string, string>() {
                    {"deviceId", SyncWithSkaldState.DeviceId}
                };

                var response = await HttpClient.PostAsync(
                    url,
                    new FormUrlEncodedContent(body)
                );

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception(
                        $"Login failed: {(int)response.StatusCode} {await response.Content.ReadAsStringAsync()}"
                    );
                }

                var data = JsonConvert.DeserializeObject<CheckChallengeResponse>(await response.Content.ReadAsStringAsync());
                if (data.Status == "verified")
                {
                    return data;
                }

                if (data.Status == "not found")
                {
                    throw new Exception("Challenge not found");
                }

                if (data.Status == "expired")
                {
                    throw new Exception("Challenge expired");
                }

                await Task.Delay(CheckInterval);
            }

            throw new Exception("Challenge expired");
        }

        private static void OpenBrowser(string url)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true,
            };

            Process.Start(startInfo);
        }

        private class InitiateChallengeResponse
        {
            [JsonProperty("challengeId")]
            public string ChallengeId { get; set; }

            [JsonProperty("verificationUrl")]
            public string VerificationUrl { get; set; }

            [JsonProperty("expiresAt")]
            public string ExpiresAt { get; set; }
        }

        private class CheckChallengeResponse
        {
            [JsonProperty("status")]
            public string Status { get; set; }

            [JsonProperty("username")]
            public string Username { get; set; }

            [JsonProperty("token")]
            public string Token { get; set; }
        }
    }
}
