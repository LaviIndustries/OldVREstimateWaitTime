namespace WaitTimeSimulatorWindowsS
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.WTSimServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.WTSimServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // WTSimServiceProcessInstaller
            // 
            this.WTSimServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalService;
            this.WTSimServiceProcessInstaller.Password = null;
            this.WTSimServiceProcessInstaller.Username = null;
            // 
            // WTSimServiceInstaller
            // 
            this.WTSimServiceInstaller.ServiceName = "WaitTimeSimulatorWindowsS";
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.WTSimServiceProcessInstaller,
            this.WTSimServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller WTSimServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller WTSimServiceInstaller;
    }
}