using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Trac
{
    [Serializable]
    public sealed class TracCategory : IssueTrackerCategory
    {
        public TracCategory(string name)
            : base(name, name, null)
        {
        }

        public override string ToString() => this.CategoryName;
    }
}
