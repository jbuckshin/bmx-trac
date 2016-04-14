using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Trac
{
    [Serializable]
    public sealed class TracIssue : IssueTrackerIssue
    {
        public TracIssue(int id, string status, string title, string description, string release)
            : base(id.ToString(), status, title, description, release)
        {
        }
    }
}
