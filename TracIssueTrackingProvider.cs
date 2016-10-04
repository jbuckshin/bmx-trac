using System;
using System.Collections.Generic;
using System.ComponentModel;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Extensibility.Providers.IssueTracking;
using Inedo.BuildMaster.Web;
using Inedo.Serialization;
using Inedo.BuildMaster.Extensibility;

namespace Inedo.BuildMasterExtensions.Trac
{
    [DisplayName("Edgewall Trac")]
    [Description("Supports Trac 0.11 and later; requires that the tracxmlrpc plugin is installed.")]
    [CustomEditor(typeof(TracIssueTrackingProviderEditor))]
    public sealed class TracIssueTrackingProvider : IssueTrackingProviderBase, ICategoryFilterable, IUpdatingProvider
    {
        private XmlRpc rpc;

        public bool CanAppendIssueDescriptions => true;
        public bool CanChangeIssueStatuses => false;
        public bool CanCloseIssues => true;
        public string[] CategoryIdFilter { get; set; }
        public string[] CategoryTypeNames => new[] { "Ticket Type" };

        [Persistent]
        public string RpcUrl { get; set; }
        [Persistent]
        public string UserName { get; set; }
        [Persistent]
        public string Password { get; set; }
        [Persistent]
        public string SubProject { get; set; }
        [Persistent]
        public bool UsesMilestoneToObtainIssues { get; set; }

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

        public override string GetIssueUrl(IssueTrackerIssue issue)
        {
            
            if (issue == null)
                throw new ArgumentNullException("issue");

            var url = this.RpcUrl;
            if (!url.EndsWith("/"))
                url += "/";

            return url + "ticket/" + issue.IssueId;
        }
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
        public IssueTrackerCategory[] GetCategories()
        {
            var categories = new List<TracCategory>();
            var typeNames = (object[])this.Rpc.Invoke("ticket.type.getAll");
            foreach (string name in typeNames)
                categories.Add(new TracCategory(name));

            return categories.ToArray();
        }
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
        public void ChangeIssueStatus(string issueId, string newStatus)
        {
            throw new NotSupportedException();
        }
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
        public override string ToString() => "Connects to the Trac ticketing system";

        private static string GetString(Dictionary<string, object> dictionary, string key)
        {
            object value;
            if (dictionary.TryGetValue(key, out value))
                return (value ?? string.Empty).ToString();
            else
                return string.Empty;
        }

        private string GetIssueFilterQuery( string releaseNumber ) {

            string query = string.Empty;

            if ( !string.IsNullOrWhiteSpace( releaseNumber ) ) {
                if ( !this.UsesMilestoneToObtainIssues )
                    query = "version=" + releaseNumber;
                else if ( !string.IsNullOrWhiteSpace( this.SubProject ) )
                    query = string.Format( "milestone={0} {1}",
                        this.SubProject, releaseNumber );
                else
                {
                    query = "milestone=" + releaseNumber;
                }
            }

            return query;
        }
    }
}
