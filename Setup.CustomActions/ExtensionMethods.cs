 
namespace Setup.CustomActions
{
    using System;
    using System.DirectoryServices;
    using System.Linq;

    using Microsoft.Web.Administration;
    using System.Security.Policy;

    /// <summary>
    /// Extension Methods for IIS
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// The IIS entry
        /// </summary>
        private const string IISEntry = "IIS://localhost/W3SVC/";

        /// <summary>
        /// The root
        /// </summary>
        private const string Root = "/root";

        /// <summary>
        /// The path
        /// </summary>
        private const string Path = "Path";

        /// <summary>
        /// Physicals the path.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns>The physical path of the site.</returns>
        /// <exception cref="System.ArgumentNullException">site was null</exception>
        public static string PhysicalPath(this Microsoft.Web.Administration.Site site)
        {
            if (site == null)
            {
                throw new ArgumentNullException("site");
            }

            var root = site.Applications.Where(a => a.Path == "/").Single();
            var virtualRoot = root.VirtualDirectories.Where(v => v.Path == "/")
                .Single();

            // Can get environment variables, so need to expand them
            return Environment.ExpandEnvironmentVariables(virtualRoot.PhysicalPath);
        }

        /// <summary>
        /// Physicals the path.
        /// </summary>
        /// <param name="site">The site.</param>
        /// <returns>The physical path of the site.</returns>
        /// <exception cref="System.ArgumentNullException">site was null</exception>
        public static string PhysicalPath(this DirectoryEntry site)
        {
            if (site == null)
            {
                throw new ArgumentNullException("site");
            }

            string path;

            using (DirectoryEntry de = new DirectoryEntry(IISEntry
                + site.Name + Root))
            {
                path = de.Properties[Path].Value.ToString();
            }

            return path;
        }
    }
}
