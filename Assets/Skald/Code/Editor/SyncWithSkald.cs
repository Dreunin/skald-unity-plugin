using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using Debug = UnityEngine.Debug;
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

        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(3);

        private const string testUrl = "http://localhost:9999";

        private const string clientId = "unity";

        public SyncWithSkald()
        {
            if (SyncWithSkaldState.IsLoggedIn)
            {
                HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", SyncWithSkaldState.Token);
            }
        }

        private static string GetDeviceId()
        {
            return "very good id"; // TODO: Replace with some semi-secret local identifier that is constant for this device
        }

        // Called by the custom inspector when the user clicks Login
        public async Awaitable Login()
        {
            var initiateChallengeResponse = await InitiateChallenge(testUrl);
            OpenBrowser(initiateChallengeResponse.VerificationUrl);
            var checkChallengeResponse = await CheckChallenge(initiateChallengeResponse.ChallengeId, DateTime.Parse(initiateChallengeResponse.ExpiresAt), testUrl);
            SyncWithSkaldState.Login(checkChallengeResponse.Token);
            HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", checkChallengeResponse.Token);
        }

        // Called by the custom inspector when the user clicks Sync
        public async Awaitable<Project[]> GetProjects()
        {
            try
            {
                Debug.Log($"Sending request to {testUrl}/{ApiProjectListUrl}");
                var response = await HttpClient.GetAsync($"{testUrl}/{ApiProjectListUrl}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"Sync response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to get projects: {responseBody}");
                }

                var projects = JsonConvert.DeserializeObject<Project[]>(responseBody);

                return projects ?? Array.Empty<Project>();
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during GetProjects: {e.Message}");
                return Array.Empty<Project>();
            }
        }

        public async Awaitable<EngineImport> LoadProject(string projectId)
        {
            try
            {
                Debug.Log($"Sending request to {testUrl}/{ApiExportProjectUrl(projectId)}");
                var response = await HttpClient.GetAsync($"{testUrl}/{ApiExportProjectUrl(projectId)}");
                var responseBody = await response.Content.ReadAsStringAsync();
                Debug.Log($"Sync response: {responseBody}");

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Failed to load project: {responseBody}");
                }

                var project = JsonConvert.DeserializeObject<EngineImport>(responseBody, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                });
                Debug.Log($"Project data: \n{project}");

                return project;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during LoadProject: {e.Message}");
                return null;
            }
        }

        public async Awaitable CreateOrUpdateProject(EngineImport project)
        {
            Debug.Log($"Creating or updating project: {project.Project.Title}");
        }

        public Awaitable Logout()
        {
            Debug.Log("Logging out");

            SyncWithSkaldState.Logout();
            return null;
        }

        private async Awaitable<InitiateChallengeResponse> InitiateChallenge(string baseUrl)
        {
            var body = new Dictionary<string, string> {
                {"deviceId", GetDeviceId()},
                {"requesterName", clientId}
            };

            Debug.Log($"{baseUrl}/{ApiInitiateChallengeUrl}");
            Debug.Log("before post");
            var response = await HttpClient.PostAsync(
                $"{baseUrl}/{ApiInitiateChallengeUrl}",
                new FormUrlEncodedContent(body)
            );
            Debug.Log("after post");
            var responseText = await response.Content.ReadAsStringAsync();
            Debug.Log(responseText);

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Login failed: {(int)response.StatusCode}"
                );
            }

            var data = JsonConvert.DeserializeObject<InitiateChallengeResponse>(responseText);
            Debug.Log("after deserialize");
            return data;
        }

        private async Awaitable<CheckChallengeResponse> CheckChallenge(string challengeId, DateTime expiresAt, string baseUrl)
        {
            var url = $"{baseUrl}/{ApiCheckChallengeUrl(challengeId)}";

            while (new DateTime() < expiresAt)
            {
                var body = new Dictionary<string, string>() {
                    {"deviceId", GetDeviceId()}
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

        private class ProjectResponse
        {


        }
    }
}
