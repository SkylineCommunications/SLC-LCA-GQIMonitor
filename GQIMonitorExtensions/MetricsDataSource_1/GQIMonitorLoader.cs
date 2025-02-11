using System;
using System.IO;
using System.Reflection;

namespace MetricsDataSource_1
{
    public abstract class GQIMonitorLoader
    {
        static GQIMonitorLoader()
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
        }

        private const string LibrariesFolder = @"C:\Skyline DataMiner\Scripts\Libraries";

        private static Assembly ResolveAssembly(object sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            if (!assemblyName.Name.StartsWith("GQIMonitor.GQIMonitor"))
                return null;

            var fileName = $"{assemblyName.Name}.dll";
            var filePath = Path.Combine(LibrariesFolder, fileName);
            try
            {
                return Assembly.LoadFile(filePath);
            }
            catch
            {
                return null;
            }
        }
    }
}
