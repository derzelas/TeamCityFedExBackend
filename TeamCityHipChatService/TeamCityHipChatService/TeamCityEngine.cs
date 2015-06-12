using System.Net;
using RestSharp;

namespace TeamCityHipChatService
{
    public class TeamCityEngine
    {
        private readonly RestClient _restClient;

        public TeamCityEngine(string teamCityServer)
        {
            _restClient = new RestClient(teamCityServer)
            {
                Authenticator = new NtlmAuthenticator(CredentialCache.DefaultNetworkCredentials)
            };
        }

        public ProjectStatus GetProjectStatus(string projectId)
        {
            var runningBuildResponse = GetRunningBuildResponse(projectId);
            var lastBuildResponse = GetLastBuildResponse(projectId);

            if (runningBuildResponse == null)
            {
                return new ProjectStatus
                {
                    State = State.Idle,
                    Status = GetStatus(lastBuildResponse)
                };
            }

            return new ProjectStatus
                {
                    State = State.Running,
                    Status = runningBuildResponse.Status == "FAILED" ? Status.Failed : GetStatus(lastBuildResponse)
                };
        }

        public void RunProjectBuild(string projectId)
        {
            var request = new RestRequest("action.html?add2Queue="+projectId, Method.GET);

            _restClient.Execute(request);
        }

        private static Status GetStatus(BuildResponse lastBuildResponse)
        {
            if (lastBuildResponse.Status == "SUCCESS")
            {
                return Status.Success;
            }

            return Status.Failed;
        }

        private BuildResponse GetRunningBuildResponse(string projectId)
        {
            var request = new RestRequest("app/rest/builds/buildType:(id:{id}),running:true", Method.GET);
            request.AddUrlSegment("id", projectId);

            IRestResponse<BuildResponse> response = _restClient.Execute<BuildResponse>(request);
            BuildResponse buildResponse = response.Data;
            return buildResponse;
        }

        private BuildResponse GetLastBuildResponse(string projectId)
        {
            var request = new RestRequest("app/rest/builds/buildType:(id:{id})", Method.GET);
            request.AddUrlSegment("id", projectId);

            IRestResponse<BuildResponse> response = _restClient.Execute<BuildResponse>(request);
            BuildResponse buildResponse = response.Data;
            return buildResponse;
        }
    }

    internal class BuildResponse
    {
        public string Status { get; set; }
        public string State { get; set; }
    }
}