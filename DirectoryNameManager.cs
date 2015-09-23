using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Org.Kevoree.ModelGenerator.Nuget
{
    class DirectoryNameManager
    {
        public static string getShadowCopyPath(string packageName, string packageVersion)
        {
            return getFullPath(packageName, packageVersion, "ShadowCopyCache");
        }

        public static string getPluginPath(string packageName, string packageVersion)
        {
            return getFullPath(packageName, packageVersion, "");
        }

        private static string getFullPath(string packageName, string version, string finalPath)
        {
            // TODO : magic path to replace
            string rootPath = @"C:\Users\mleduc\Desktop\NUGET";
            //string rootPath = @"C:\Users\mleduc\Documents\GitHub\kevoree-dotnet-comp-yield-version\bin\Debug";
            var path = Path.Combine(rootPath, packageName, version, finalPath);

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            return path;
        }
    }
}
