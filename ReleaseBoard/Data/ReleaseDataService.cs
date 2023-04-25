using Microsoft.Extensions.Options;
using Microsoft.TeamFoundation.Build.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi.Clients;
using Microsoft.VisualStudio.Services.WebApi;

namespace ReleaseBoard.Data
{
    public class ReleaseDataService
    {
        private readonly DevOps _devOps;
        public ReleaseDataService(IOptions<DevOps> options)
        {
            _devOps = options.Value;
        }

        public Uri AzureDevOpsOrganizationUrl
        {
            get
            {
                return new Uri($"https://vsrm.dev.azure.com/{_devOps.OrgName}/");
            }
        }

        private VssConnection _connection;
        public VssConnection Connection
        {
            get
            {
                if (_connection == null)
                    _connection = new VssConnection(AzureDevOpsOrganizationUrl, new VssBasicCredential(string.Empty, _devOps.PAT));
                return _connection;
            }
        }

        public async Task<List<Release>> GetReleasesAsync(int lastDays = 30)
        {
            var endDate = DateTime.Today.ToUniversalTime();
            var startDate = endDate.AddDays(-1 * lastDays);
            var releaseClient = Connection.GetClient<ReleaseHttpClient>();
            return await releaseClient.GetReleasesAsync(project: _devOps.ProjectName, minCreatedTime: startDate, definitionId: _devOps.DefinitionId) ?? new List<Release>();
        }

        public async IAsyncEnumerable<List<WorkItem>> GetReleaseWorkItemsAsync(int releaseId)
        {
            var releaseClient = Connection.GetClient<ReleaseHttpClient>();
            var buildClient = Connection.GetClient<BuildHttpClient>();
            var wiclient = Connection.GetClient<WorkItemTrackingHttpClient>();
            var release = await releaseClient.GetReleaseAsync(project: _devOps.ProjectName, releaseId: releaseId);
            var deploymentArtifact = release.Artifacts.FirstOrDefault();

            if (deploymentArtifact == null)
                yield break;

            var buildRunId = Convert.ToInt32(deploymentArtifact.DefinitionReference["version"].Id);
            var workItemList = await buildClient.GetBuildWorkItemsRefsAsync(project: _devOps.ProjectName, buildRunId);

            List<int> ints= new List<int>();
            foreach ( var workItem in workItemList) {
                int id = 0;
                if (int.TryParse(workItem.Id, out id))
                {
                    ints.Add(id);
                }
            }

            foreach (var ids in ints.Chunk(100))
            {
                WorkItemBatchGetRequest req = new WorkItemBatchGetRequest();
                req.Ids = ids;
                req.Fields = new List<string> {
                    "System.BoardColumn","System.IterationId","System.AssignedTo","System.BoardColumn","System.Title","System.BoardColumnDone"
                };
                yield return await wiclient.GetWorkItemsBatchAsync(req);
            }
        }
    }
}