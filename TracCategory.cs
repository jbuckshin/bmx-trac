using System;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;

namespace Inedo.BuildMasterExtensions.Trac
{
    /// <summary>
    /// Category for the Trac issue tracker.
    /// </summary>
    [Serializable]
    public sealed class TracCategory : IssueTrackerCategory
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TracCategory"/> class.
        /// </summary>
        /// <param name="name">The ticket name.</param>
        public TracCategory(string name)
            : base(name, name, null)
        {
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return this.CategoryName;
        }
    }
}
