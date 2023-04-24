using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.ReleaseManagement.WebApi;

namespace ReleaseBoard.Data
{
    public class DevOps
    {
        public string OrgName { get; set; }
        public string ProjectName { get; set; }
        public string PAT { get; set; }
        public int DefinitionId { get; set; }
    }

    public class ReleaseItem
    {
        public Release Release { get; set; }
        public bool IsCollapsed { get; set; }
        public List<WorkItem> WorkItems { get; set; }
    }
}