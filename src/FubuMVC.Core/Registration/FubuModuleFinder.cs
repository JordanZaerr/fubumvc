﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using FubuCore;
using FubuCore.Reflection;
using FubuMVC.Core.Diagnostics.Packaging;

namespace FubuMVC.Core.Registration
{
    public static class FubuModuleFinder
    {
        public static IEnumerable<Assembly> FindModuleAssemblies(IActivationDiagnostics diagnostics)
        {
            return findAssemblies(file =>
            {
                diagnostics.LogFor(typeof(FubuRuntime)).Trace("Unable to load assembly from file " + file);
            }).Where(x => x.HasAttribute<FubuModuleAttribute>()).ToArray();
        }

        public static IEnumerable<Assembly> FindDependentAssemblies()
        {
            return findAssemblies(file => { }).Where(x => x.GetReferencedAssemblies().Any(assem => assem.Name == "FubuMVC.Core"));
        } 

        private static IEnumerable<Assembly> findAssemblies(Action<string> logFailure)
        {
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            var binPath = FindBinPath();
            if (binPath.IsNotEmpty())
            {
                assemblyPath = assemblyPath.AppendPath(binPath);
            }


            var files = new FileSystem().FindFiles(assemblyPath, FileSet.Deep("*.dll"));
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);
                Assembly assembly = null;

                try
                {
                    assembly = AppDomain.CurrentDomain.Load(name);
                }
                catch (Exception)
                {
                    logFailure(file);
                }

                if (assembly != null) yield return assembly;
            }
        }

        public static string FindBinPath()
        {
            var binPath = AppDomain.CurrentDomain.SetupInformation.PrivateBinPath;
            if (binPath.IsNotEmpty())
            {
                return Path.IsPathRooted(binPath)
                    ? binPath
                    : AppDomain.CurrentDomain.SetupInformation.ApplicationBase.AppendPath(binPath);
            }

            return null;
        }
    }
}