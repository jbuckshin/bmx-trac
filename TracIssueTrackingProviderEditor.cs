using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;
using Inedo.Web.Controls.SimpleHtml;

namespace Inedo.BuildMasterExtensions.Trac
{
    internal sealed class TracIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUrl;
        private ValidatingTextBox txtUser;
        private PasswordTextBox txtPassword;
        private ValidatingTextBox txtSubProject;
        private CheckBox chkUsesMilestones;

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (TracIssueTrackingProvider)extension;
            this.txtUrl.Text = provider.RpcUrl;
            this.txtUser.Text = provider.UserName;
            this.txtPassword.Text = provider.Password;
            this.chkUsesMilestones.Checked = provider.UsesMilestoneToObtainIssues;
            this.txtSubProject.Text = provider.SubProject;
        }

        public override ProviderBase CreateFromForm()
        {
            return new TracIssueTrackingProvider
            {
                RpcUrl = this.txtUrl.Text,
                UserName = this.txtUser.Text,
                Password = this.txtPassword.Text,
                UsesMilestoneToObtainIssues = this.chkUsesMilestones.Checked,
                SubProject = this.txtSubProject.Text
                
            };
        }

        protected override void CreateChildControls()
        {
            this.txtUrl = new ValidatingTextBox { Required = true };

            this.txtUser = new ValidatingTextBox();

            this.txtPassword = new PasswordTextBox();

            this.txtSubProject = new ValidatingTextBox();

            this.chkUsesMilestones = new CheckBox();

            this.Controls.Add(
                new SlimFormField("URL:", this.txtUrl)
                {
                    HelpText = "The address of the Trac project. For example: http://tracserv/trac/Project"
                },
                new SlimFormField("User name:", this.txtUser),
                new SlimFormField("Password:", this.txtPassword),
                new SlimFormField("Use Milestones (Buildmaster release number = Trac Milestone)", new Div(this.chkUsesMilestones)),
                new SlimFormField("Subproject name (Used only when 'Use Milestones' checked):", new Div(this.txtSubProject))
            );
        }
    }
}
