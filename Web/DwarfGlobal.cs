using System;
using System.Web;
using System.Web.Configuration;
using Evergreen.Dwarf.Utilities;

namespace Evergreen.Dwarf.Web
{
    /// <summary>
    /// Implements default Global.asax functions for Dwarf projects
    /// </summary>
    public abstract class DwarfGlobal : HttpApplication
    {
        #region Methods

        #region GetConnectionString

        /// <summary>
        /// Extracts and verifies the connection string from the supplied ConnectionStringSettings
        /// </summary>
        public string GetConnectionString(string csName)
        {
            var connection = WebConfigurationManager.ConnectionStrings[csName];

            if (connection == null || string.IsNullOrEmpty(connection.ConnectionString))
                throw new NullReferenceException("Can't find a valid connectionString called " + csName);

            return connection.ConnectionString;
        }

        #endregion GetConnectionString

        #region IsConfigured

        /// <summary>
        /// Returns true if there's an active configuration for the supplied type's domain
        /// </summary>
        public bool IsConfigured<T>()
        {
            return DwarfContext<T>.IsConfigured();
        }

        #endregion IsConfigured

        #region Application_Start

        protected void Application_Start(object sender, EventArgs e)
        {
            ApplicationStart();
        }

        #endregion Application_Start

        #region ApplicationStart

        protected abstract void ApplicationStart();

        #endregion ApplicationStart

        #region Application_EndRequest

        /// <summary>
        /// See base
        /// </summary>
        public void Application_EndRequest(object sender, EventArgs e)
        {
            CacheManager.DisposeUserCache();
            ExceptionHandler.ClearErrors();
            DwarfContext.DisposeContexts();
        }

        #endregion Application_EndRequest

        #region Application_Error

        /// <summary>
        /// See base
        /// </summary>
        public void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
            if (Server.GetLastError() != null)
            {
                var exception = Server.GetLastError().InnerException ?? Server.GetLastError();

                if (exception == null)
                    return;

                //If true, then it's probably a missing file...
                if (exception is HttpException)
                    exception = new Exception("The file: \"" + Request.RawUrl + "\" is missing.", exception);

                if (!ExceptionHandler.HasCaught(exception))
                    HandleException(exception);
            }
        }

        #endregion Application_Error

        #region HandleException

        /// <summary>
        /// Handle the exception
        /// </summary>
        public abstract void HandleException(Exception ex);

        #endregion HandleException

        #endregion Methods
    }
}
