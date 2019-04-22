// <auto-generated />
namespace Microsoft.AspNetCore.Hosting
{
    using System.Globalization;
    using System.Reflection;
    using System.Resources;

    internal static class Resources
    {
        private static readonly ResourceManager _resourceManager
            = new ResourceManager("Microsoft.AspNetCore.Hosting.Resources", typeof(Resources).GetTypeInfo().Assembly);

        /// <summary>
        /// Internal Server Error
        /// </summary>
        internal static string ErrorPageHtml_Title
        {
            get => GetString("ErrorPageHtml_Title");
        }

        /// <summary>
        /// Internal Server Error
        /// </summary>
        internal static string FormatErrorPageHtml_Title()
            => GetString("ErrorPageHtml_Title");

        /// <summary>
        /// An error occurred while starting the application.
        /// </summary>
        internal static string ErrorPageHtml_UnhandledException
        {
            get => GetString("ErrorPageHtml_UnhandledException");
        }

        /// <summary>
        /// An error occurred while starting the application.
        /// </summary>
        internal static string FormatErrorPageHtml_UnhandledException()
            => GetString("ErrorPageHtml_UnhandledException");

        /// <summary>
        /// Unknown location
        /// </summary>
        internal static string ErrorPageHtml_UnknownLocation
        {
            get => GetString("ErrorPageHtml_UnknownLocation");
        }

        /// <summary>
        /// Unknown location
        /// </summary>
        internal static string FormatErrorPageHtml_UnknownLocation()
            => GetString("ErrorPageHtml_UnknownLocation");

        /// <summary>
        /// WebHostBuilder allows creation only of a single instance of WebHost
        /// </summary>
        internal static string WebHostBuilder_SingleInstance
        {
            get => GetString("WebHostBuilder_SingleInstance");
        }

        /// <summary>
        /// WebHostBuilder allows creation only of a single instance of WebHost
        /// </summary>
        internal static string FormatWebHostBuilder_SingleInstance()
            => GetString("WebHostBuilder_SingleInstance");

        private static string GetString(string name, params string[] formatterNames)
        {
            var value = _resourceManager.GetString(name);

            System.Diagnostics.Debug.Assert(value != null);

            if (formatterNames != null)
            {
                for (var i = 0; i < formatterNames.Length; i++)
                {
                    value = value.Replace("{" + formatterNames[i] + "}", "{" + i + "}");
                }
            }

            return value;
        }
    }
}
