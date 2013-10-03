using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Trac
{
    /// <summary>
    /// An issue in the Trac issue tracking system.
    /// </summary>
    [Serializable]
    public sealed class TracIssue : Issue
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TracIssue"/> class.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="status">The status.</param>
        /// <param name="title">The title.</param>
        /// <param name="description">The description.</param>
        /// <param name="release">The release.</param>
        public TracIssue(int id, string status, string title, string description, string release)
            : base(id.ToString(), status, title, description, release)
        {
        }
    }
}
