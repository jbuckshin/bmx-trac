using System;
using System.Collections.Generic;
using Inedo.BuildMaster;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;

namespace Inedo.BuildMasterExtensions.Trac
{
    /// <summary>
    /// Issue tracking provider for Edgewall Trac.
    /// </summary>
    [ProviderProperties(
      "Edgewall Trac",
      "Supports Trac 0.11 and later; requires that the tracxmlrpc plugin is installed.")]
    [CustomEditor(typeof(TracIssueTrackingProviderEditor))]
    public sealed class TracIssueTrackingProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        private XmlRpc rpc;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracIssueTrackingProvider"/> class.
        /// </summary>
        public TracIssueTrackingProvider()
        {
        }

        /// <summary>
        /// Gets a value indicating whether an issue's description can be appended to.
        /// </summary>
        public bool CanAppendIssueDescriptions
        {
            get { return true; }
        }
        /// <summary>
        /// Gets a value indicating whether an issue's status can be changed.
        /// </summary>
        public bool CanChangeIssueStatuses
        {
            get { return false; }
        }
        /// <summary>
        /// Gets a value indicating whether an issue can be closed.
        /// </summary>
        public bool CanCloseIssues
        {
            get { return true; }
        }
        /// <summary>
        /// Gets or sets the category id filter.
        /// </summary>
        public string[] CategoryIdFilter { get; set; }
        /// <summary>
        /// Gets an inheritor-defined array of category types.
        /// </summary>
        public string[] CategoryTypeNames
        {
            get { return new[] { "Ticket Type" }; }
        }

        /// <summary>
        /// Gets or sets the URL of the XML-RPC service.
        /// </summary>
        /// <example>
        /// http://bmtracsv1/trac/Project
        /// </example>
        [Persistent]
        public string RpcUrl { get; set; }
        /// <summary>
        /// Gets or sets the username for authenticating with the RPC service.
        /// </summary>
        [Persistent]
        public string UserName { get; set; }
        /// <summary>
        /// Gets or sets the password for authenticating with the RPC service.
        /// </summary>
        [Persistent]
        public string Password { get; set; }
        /// <summary>
        /// Gets or sets sub-project name in multi-project trac environment
        /// </summary>
        [Persistent]
        public string SubProject { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether provider should use milestones to obtain issues
        /// </summary>
        [Persistent]
        public bool UsesMilestoneToObtainIssues { get; set; }

        /// <summary>
        /// Gets the XML-RPC proxy object.
        /// </summary>
        private XmlRpc Rpc
        {
            get
            {
                if (this.rpc == null)
                {
                    Uri uri;
                    var url = this.RpcUrl;
                    if (!url.EndsWith("/"))
                        url += "/";

                    if (string.IsNullOrEmpty(this.UserName))
                        uri = new Uri(new Uri(url), "rpc");
                    else
                        uri = new Uri(new Uri(url), "login/rpc");

                    this.rpc = new XmlRpc(uri, this.UserName, this.Password);
                }

                return this.rpc;
            }
        }

        /// <summary>
        /// Gets a URL to the specified issue.
        /// </summary>
        /// <param name="issue">The issue whose URL is returned.</param>
        /// <returns>
        /// The URL of the specified issue if applicable; otherwise null.
        /// </returns>
        public override string GetIssueUrl(IssueTrackerIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            var url = this.RpcUrl;
            if (!url.EndsWith("/"))
                url += "/";

            return url + "ticket/" + issue.IssueId;
        }
        /// <summary>
        /// Returns an array of <see cref="Issue"/> objects that are for the current
        /// release.
        /// </summary>
        /// <param name="releaseNumber">Release number of issues to return.</param>
        /// <returns>
        /// Array of issues for the specified release.
        /// </returns>
        public override IssueTrackerIssue[] GetIssues(string releaseNumber)
        {
            var queries = new List<string>();
            string filterQuery = this.GetIssueFilterQuery( releaseNumber );
            if ( !string.IsNullOrEmpty( filterQuery ) )
                queries.Add( filterQuery );

            if (this.CategoryIdFilter != null && this.CategoryIdFilter.Length > 0 && !string.IsNullOrEmpty(this.CategoryIdFilter[0]))
                queries.Add("type=" + this.CategoryIdFilter[0]);

            queries.Add("max=0");

            var query = string.Join("&", queries.ToArray());
            var issues = new List<TracIssue>();
            var issueIds = (object[])this.Rpc.Invoke("ticket.query", query);
            foreach (int id in issueIds)
            {
                var ticket = (object[])this.Rpc.Invoke("ticket.get", id);
                var attributes = (Dictionary<string, object>)ticket[3];
                issues.Add(new TracIssue(id, GetString(attributes, "status"), GetString(attributes, "summary"), GetString(attributes, "description"), releaseNumber));
            }

            return issues.ToArray();
        }

        /// <summary>
        /// Returns a value indicating if the specified issue is closed.
        /// </summary>
        /// <param name="issue">Issue to check for a closed state.</param>
        /// <returns>
        /// True if issue is closed; otherwise false.
        /// </returns>
        public override bool IsIssueClosed(IssueTrackerIssue issue)
        {
            if (issue == null)
                throw new ArgumentNullException("issue");

            return string.Equals(issue.IssueStatus, "closed", StringComparison.CurrentCultureIgnoreCase);
        }
        public override bool IsAvailable()
        {
            return true;
        }
        public override void ValidateConnection()
        {
            var version = (object[])this.Rpc.Invoke("system.getAPIVersion");
            if (version == null || version.Length == 0)
                throw new NotAvailableException();
        }
        /// <summary>
        /// Returns an array of all appropriate categories defined within the provider.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// The nesting level (i.e. <see cref="CategoryBase.SubCategories"/>) can never be less than
        /// the length of <see cref="CategoryTypeNames"/>.
        /// </remarks>
        public IssueTrackerCategory[] GetCategories()
        {
            var categories = new List<TracCategory>();
            var typeNames = (object[])this.Rpc.Invoke("ticket.type.getAll");
            foreach (string name in typeNames)
                categories.Add(new TracCategory(name));

            return categories.ToArray();
        }
        /// <summary>
        /// Appends the specified text to the specified issue.
        /// </summary>
        /// <param name="issueId">Id of the issue.</param>
        /// <param name="textToAppend">Text to append to the issue description.</param>
        public void AppendIssueDescription(string issueId, string textToAppend)
        {
            if (string.IsNullOrEmpty(issueId))
                throw new ArgumentNullException("issueId");
            if (string.IsNullOrEmpty(textToAppend))
                return;

            int id = int.Parse(issueId);
            var ticket = (object[])this.Rpc.Invoke("ticket.get", id);
            var attributes = (Dictionary<string, object>)ticket[3];
            var description = GetString(attributes, "description") + textToAppend;

            var updatedFields = new Dictionary<string, object>()
            {
                { "description", description }
            };

            var result = this.Rpc.Invoke("ticket.update", id, string.Empty, updatedFields);
            if (result == null)
                throw new InvalidOperationException();
        }
        /// <summary>
        /// Changes the specified issue's status
        /// </summary>
        /// <param name="issueId">Id of the issue.</param>
        /// <param name="newStatus">New status of the issue.</param>
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            throw new NotSupportedException();
        }
        /// <summary>
        /// Closes the specified issue.
        /// </summary>
        /// <param name="issueId">Id of the issue.</param>
        public void CloseIssue(string issueId)
        {
            if (string.IsNullOrEmpty("issueId"))
                throw new ArgumentNullException("issueId");

            int id = int.Parse(issueId);

            var updatedFields = new Dictionary<string, object>()
            {
                { "action", "resolve" }
            };

            this.Rpc.Invoke("ticket.update", id, string.Empty, updatedFields);
        }
        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return "Connects to the Trac ticketing system.";
        }

        /// <summary>
        /// Returns a dictionary value as a string.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        /// <param name="key">The key in the dictionary.</param>
        /// <returns>Value of the specified key or an empty string if the key was not found.</returns>
        private static string GetString(Dictionary<string, object> dictionary, string key)
        {
            object value;
            if (dictionary.TryGetValue(key, out value))
                return (value ?? string.Empty).ToString();
            else
                return string.Empty;
        }

        /// <summary>
        /// Returns a query that obtains issues by version or milestone
        /// </summary>
        /// <param name="releaseNumber">Release number of issues to return.</param>
        /// <returns>Query that can be used to obtain list of issues from trac environment</returns>
        private string GetIssueFilterQuery( string releaseNumber ) {
            string query = string.Empty;
            if ( !string.IsNullOrWhiteSpace( releaseNumber ) ) {
                if ( !this.UsesMilestoneToObtainIssues )
                    query = "version=" + releaseNumber;
                else if ( !string.IsNullOrWhiteSpace( this.SubProject ) )
                    query = string.Format( "milestone={0} {1}",
                        this.SubProject, releaseNumber );
            }
            return query;
        }
    }
}
