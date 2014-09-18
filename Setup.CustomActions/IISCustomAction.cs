 
namespace Setup.CustomActions
{
    using System;
    using System.DirectoryServices;
    using System.Globalization;
    using System.Management;

    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Web.Administration;
    using Microsoft.Win32;

    /// <summary>
    /// Class offering WiX custom actions for manipulating IIS 6 and 7.
    /// </summary>
    public static class IISCustomAction
    {
        #region Private Constants
        /// <summary>
        /// The IIS entry
        /// </summary>
        private const string IISEntry = "IIS://localhost/W3SVC";

        /// <summary>
        /// The session entry
        /// </summary>
        private const string SessionEntry = "WEBSITE";

        /// <summary>
        /// The server bindings
        /// </summary>
        private const string ServerBindings = "ServerBindings";

        /// <summary>
        /// The server comment
        /// </summary>
        private const string ServerComment = "ServerComment";

        /// <summary>
        /// The custom action exception
        /// </summary>
        private const string CustomActionException = "CustomActionException: ";

        /// <summary>
        /// The IIS registry key
        /// </summary>
        private const string IISRegKey = @"Software\Microsoft\InetStp";

        /// <summary>
        /// The major version
        /// </summary>
        private const string MajorVersion = "MajorVersion";

        /// <summary>
        /// The IIS web server
        /// </summary>
        private const string IISWebServer = "iiswebserver";

        /// <summary>
        /// The get combo content
        /// </summary>
        private const string GetComboContent = "select * from ComboBox";

        /// <summary>
        /// The available sites
        /// </summary>
        private const string AvailableSites
            = "select * from AvailableWebSites";

        /// <summary>
        /// The specific site
        /// </summary>
        private const string SpecificSite
            = "Select * from AvailableWebSites where WebSiteID=";
        #endregion

        /// <summary>
        /// Gets a value indicating whether this instance is II s7 upwards.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is II s7 upwards; otherwise, <c>false</c>.
        /// </value>
        private static bool IsIIS7Upwards
        {
            get
            {

                bool isIIS7Upwards = false;

                using (RegistryKey iisKey = Registry.LocalMachine.OpenSubKey(IISRegKey))
                {
                    if (iisKey != null)
                    {
                        isIIS7Upwards = (int)iisKey.GetValue(MajorVersion) >= 7;
                    }
                }

                return isIIS7Upwards;
            }
        }

        #region Custom Action Methods
        /// <summary>
        /// Gets the web sites.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>The Websites</returns>
        /// <exception cref="System.ArgumentNullException">Argument session was null.</exception>
        [CustomAction]
        public static ActionResult GetWebSites(Session session)
        {
            ActionResult result = ActionResult.Failure;

            try
            {
                if (session == null)
                { 
                    throw new ArgumentNullException("session");
                }

                View comboBoxView = session.Database.OpenView(GetComboContent);
                View availableWSView = session.Database.OpenView(AvailableSites);

                if (IsIIS7Upwards)
                {
                    GetWebSitesViaWebAdministration(comboBoxView, availableWSView);
                }
                else
                {
                    GetWebSitesViaMetabase(comboBoxView, availableWSView);
                }

                result = ActionResult.Success;
            }
            catch (Exception ex)
            {
                if (session != null)
                {
                    session.Log(CustomActionException + ex.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Updates the props with selected web site.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>The result of the action.</returns>
        /// <exception cref="System.ArgumentNullException">Argument session was null.</exception>
        [CustomAction]
        public static ActionResult UpdatePropsWithSelectedWebSite(Session session)
        {
            ActionResult result = ActionResult.Failure;

            try
            {
                if (session == null)
                {
                    throw new ArgumentNullException("session"); 
                }

                string selectedWebSiteId = session[SessionEntry];
                session.Log("CA: Found web site id: " + selectedWebSiteId);

                using (View availableWebSitesView = session.Database.OpenView(SpecificSite + selectedWebSiteId))
                {
                    availableWebSitesView.Execute();

                    using (Record record = availableWebSitesView.Fetch())
                    {
                        if (record[1].ToString() == selectedWebSiteId)
                        {
                            session["WEBSITE_ID"] = selectedWebSiteId;
                            session["WEBSITE_DESCRIPTION"] = (string)record[2];
                            session["WEBSITE_PATH"] = (string)record[3];
                        }
                    }
                }

                result = ActionResult.Success;
            }
            catch (Exception ex)
            {
                if (session != null)
                {
                    session.Log(CustomActionException + ex.ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Starts the application pool.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>An ActionResult</returns>
        [CustomAction]
        public static ActionResult StartApplicationPool(Session session)
        {
            ActionResult result = ActionResult.Success;
            //// StartApplicationPool does not function on the DEV service for some reason.
            return result;

            string name = session["VD"] + session["WEBSITE_ID"];

            if (IsIIS7Upwards)
            {
                StartStopApplicationPoolViaWebAdministration(name, true);
            }
            else
            {
                StartStopApplicationPoolViaWMI(name, true);
            }

            return result;
        }

        /// <summary>
        /// Stops the application pool.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>An ActionResult</returns>
        [CustomAction]
        public static ActionResult StopApplicationPool(Session session)
        {
            ActionResult result = ActionResult.Success;
            //// StopApplicationPool does not function on the DEV or UAT servers for some reason.
            return result;

            string name = session["VD"] + session["WEBSITE_ID"];

            if (IsIIS7Upwards)
            {
                StartStopApplicationPoolViaWebAdministration(name, false);
            }
            else
            {
                StartStopApplicationPoolViaWMI(name, false);
            }

            return result;
        }
        #endregion

        #region Private Helper Methods

        /// <summary>
        /// Starts the stop application pool via web administration.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="start">if set to <c>true</c> [start].</param>
        /// <returns>The action result.</returns>
        private static bool StartStopApplicationPoolViaWebAdministration(string name, bool start)
        {
            using (ServerManager iisManager = new ServerManager())
            {
                foreach (var appPool in iisManager.ApplicationPools)
                {
                    if (appPool.Name == name)
                    {
                        if (start)
                        {
                            appPool.Start();
                        }
                        else
                        {
                            appPool.Stop();
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Starts the stop application pool via WMI.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="start">if set to <c>true</c> [start].</param>
        /// <returns>The action result.</returns>
        private static bool StartStopApplicationPoolViaWMI(string name, bool start)
        {
            ManagementScope scope = new ManagementScope("root\\MicrosoftIISv2");
            scope.Connect();

            string appPoolPath = string.Format("IIsApplicationPool.Name='W3SVC/AppPools/{0}'", name);
            using (ManagementObject appPool = new ManagementObject(scope, new ManagementPath(appPoolPath), null))
            {
                if (appPool != null)
                {
                    if (start)
                    {
                        appPool.InvokeMethod("Start", null, null);
                    }
                    else
                    {
                        appPool.InvokeMethod("Stop", null, null);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the web sites via web administration.
        /// </summary>
        /// <param name="comboView">The combo view.</param>
        /// <param name="availableView">The available view.</param>
        private static void GetWebSitesViaWebAdministration(View comboView, View availableView)
        {
            using (ServerManager iisManager = new ServerManager())
            {
                int order = 1;

                foreach (Site webSite in iisManager.Sites)
                {
                    string id = webSite.Id.ToString(CultureInfo.InvariantCulture);
                    string name = webSite.Name;
                    string path = webSite.PhysicalPath();

                    StoreSiteDataInComboBoxTable(id, name, path, order++, comboView);
                    StoreSiteDataInAvailableSitesTable(id, name, path, availableView);
                }
            }
        }

        /// <summary>
        /// Gets the web sites via meta base.
        /// </summary>
        /// <param name="comboView">The combo view.</param>
        /// <param name="availableView">The available view.</param>
        private static void GetWebSitesViaMetabase(View comboView, View availableView)
        {
            using (DirectoryEntry iisRoot = new DirectoryEntry(IISEntry))
            {
                int order = 1;

                foreach (DirectoryEntry webSite in iisRoot.Children)
                {
                    if (webSite.SchemaClassName.ToLower(CultureInfo.InvariantCulture) == IISWebServer)
                    {
                        string id = webSite.Name;
                        string name = webSite.Properties[ServerComment].Value.ToString();
                        string path = webSite.PhysicalPath();

                        StoreSiteDataInComboBoxTable(id, name, path, order++, comboView);
                        StoreSiteDataInAvailableSitesTable(id, name, path, availableView);
                    }
                }
            }
        }

        /// <summary>
        /// Stores the site data in combo box table.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <param name="order">The order.</param>
        /// <param name="comboView">The combo view.</param>
        private static void StoreSiteDataInComboBoxTable(string id, string name, string physicalPath, int order, View comboView)
        {
            Record newComboRecord = new Record(5);
            newComboRecord[1] = SessionEntry;
            newComboRecord[2] = order;
            newComboRecord[3] = id;
            newComboRecord[4] = name;
            newComboRecord[5] = physicalPath;
            comboView.Modify(ViewModifyMode.InsertTemporary, newComboRecord);
        }

        /// <summary>
        /// Stores the site data in available sites table.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="name">The name.</param>
        /// <param name="physicalPath">The physical path.</param>
        /// <param name="availableView">The available view.</param>
        private static void StoreSiteDataInAvailableSitesTable(string id, string name, string physicalPath, View availableView)
        {
            Record newWebSiteRecord = new Record(3);
            newWebSiteRecord[1] = id;
            newWebSiteRecord[2] = name;
            newWebSiteRecord[3] = physicalPath;
            availableView.Modify(ViewModifyMode.InsertTemporary, newWebSiteRecord);
        }

        #endregion
    }
}
