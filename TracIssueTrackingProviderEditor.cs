using System.Web.UI.WebControls;
using Inedo.BuildMaster.Extensibility.Providers;
using Inedo.BuildMaster.Web.Controls;
using Inedo.BuildMaster.Web.Controls.Extensions;
using Inedo.Web.Controls;

namespace Inedo.BuildMasterExtensions.Trac
{
    /// <summary>
    /// Custom editor for the Trac issue tracking provider.
    /// </summary>
    internal sealed class TracIssueTrackingProviderEditor : ProviderEditorBase
    {
        private ValidatingTextBox txtUrl;
        private TextBox txtUser;
        private PasswordTextBox txtPassword;
        private TextBox txtSubProject;
        private CheckBox chkbxUsesMilestones;

        /// <summary>
        /// Initializes a new instance of the <see cref="TracIssueTrackingProviderEditor"/> class.
        /// </summary>
        public TracIssueTrackingProviderEditor()
        {
        }

        public override void BindToForm(ProviderBase extension)
        {
            var provider = (TracIssueTrackingProvider)extension;
            this.txtUrl.Text = provider.RpcUrl ?? string.Empty;
            this.txtUser.Text = provider.UserName ?? string.Empty;
            this.txtPassword.Text = provider.Password ?? string.Empty;
            this.txtSubProject.Text = provider.SubProject ?? string.Empty;
            this.chkbxUsesMilestones.Checked = provider.UsesMilestoneToObtainIssues;
        }
        public override ProviderBase CreateFromForm()
        {
            return new TracIssueTrackingProvider()
            {
                RpcUrl = this.txtUrl.Text,
                UserName = this.txtUser.Text,
                Password = this.txtPassword.Text,
                SubProject = this.txtSubProject.Text,
                UsesMilestoneToObtainIssues = this.chkbxUsesMilestones.Checked
            };
        }

        /// <summary>
        /// Called by the ASP.NET page framework to notify server controls that use composition-based
        /// implementation to create any child controls they contain in preparation for posting back or rendering.
        /// </summary>
        protected override void CreateChildControls()
        {
            base.CreateChildControls();

            this.txtUrl = new ValidatingTextBox()
            {
                Required = true,
                Width = 300
            };

            this.txtUser = new TextBox()
            {
                Width = 300
            };

            this.txtPassword = new PasswordTextBox()
            {
                Width = 270
            };

            this.txtSubProject = new TextBox() {
                Width = 300
            };

            this.chkbxUsesMilestones = new CheckBox();

            CUtil.Add(this,
                new FormFieldGroup(
                    "Trac Project URL",
                    "The address of the Trac project. For example: http://tracserv/trac/Project",
                    false,
                    new StandardFormField("URL:", this.txtUrl)
                ),
                new FormFieldGroup(
                    "Authentication",
                    "The credentials used to log in to the Trac server. For anonymous access, leave these fields blank.",
                    false,
                    new StandardFormField("User Name:", this.txtUser),
                    new StandardFormField("Password:", this.txtPassword)
                ),
                new FormFieldGroup(
                    "Multi-project",
                    "Advanced settings used when using multi-project Trac environment. For single-project environments, leave default values.",
                    false,
                    new StandardFormField("Sub-project name:", this.txtSubProject),
                    new StandardFormField("Uses milestones to obtain issues:", this.chkbxUsesMilestones)
                )
            );
        }
    }
}
