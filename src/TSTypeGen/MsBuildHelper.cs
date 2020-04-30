using Microsoft.Build.Locator;
using System;
using System.Linq;

namespace TSTypeGen
{
    internal class MSBuildHelper
    {
        internal static bool TryQueryMSBuildMajorVersions(out string versions)
        {
            var retval = false;
            try
            {
                var instances = MSBuildLocator.QueryVisualStudioInstances()
                    .Select(vs => vs.Version.Major.ToString()).Append("Current").OrderByDescending(s => s);

                versions = string.Join(", ", instances);
                retval = true;
            }
            catch
            {
                versions = string.Empty;
            }

            return retval;
        }

        internal static bool TryRegisterMSBuildVersion(string version)
        {
            var retval = false;
            try
            {
                if (version.ToLowerInvariant() == "current")
                {
                    var current = MSBuildLocator.QueryVisualStudioInstances().OrderByDescending(vs => vs.Version.Major).FirstOrDefault();
                    MSBuildLocator.RegisterInstance(current);

                    retval = true;
                }

                else if (int.TryParse(version, out var major))
                {
                    var instance = MSBuildLocator.QueryVisualStudioInstances().FirstOrDefault(vs => vs.Version.Major == major);
                    MSBuildLocator.RegisterInstance(instance);

                    retval = true;
                }
            }
            catch
            {
                retval = false;
            }

            return retval;
        }

        internal static bool TryRegisterMSBuildPath(string msBuildPath)
        {
            bool retval;
            try
            {
                MSBuildLocator.RegisterMSBuildPath(msBuildPath);

                retval = true;
            }
            catch
            {
                retval = false;
            }

            return retval;
        }
    }
}
