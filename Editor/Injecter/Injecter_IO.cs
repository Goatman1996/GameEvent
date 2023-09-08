using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using System.Linq;

namespace GameEvent
{
    public partial class Injecter
    {
        private FileStream dllStream;
        private AssemblyDefinition assemblyDefinition;

        private string backeupDir;
        private string bakeDllPath;
        private string bakPdbPath;

        private void Initialize_IO()
        {
            this.backeupDir = Path.GetDirectoryName(this.dllPath) + "/Temp";

            this.bakeDllPath = $"{this.backeupDir}/bak.dll";
            this.bakPdbPath = $"{this.backeupDir}/bak.pdb";
        }

        private void BackUpDll()
        {
            if (Directory.Exists(this.backeupDir))
            {
                Directory.Delete(this.backeupDir, true);
            }
            Directory.CreateDirectory(this.backeupDir);

            File.Copy(this.dllPath, this.bakeDllPath);
            File.Copy(this.pdbPath, this.bakPdbPath);
        }

        private DefaultAssemblyResolver CreateAssemblyResolver()
        {
            HashSet<string> searchDir = new HashSet<string>();
            foreach (var path in (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                  select Path.GetDirectoryName(asm.ManifestModule.FullyQualifiedName)).Distinct())
            {
                try
                {
                    // UnityEngine.Debug.Log(path);
                    if (searchDir.Contains(path) == false)
                    {
                        searchDir.Add(path);
                    }
                }
                catch { }
            }

            DefaultAssemblyResolver resole = new DefaultAssemblyResolver();
            foreach (var referenceDir in searchDir)
            {
                resole.AddSearchDirectory(referenceDir);
            }

            return resole;
        }

        private void ReadDll()
        {
            this.dllStream = new FileStream(this.bakeDllPath, FileMode.Open);

            var assemblyResolver = this.CreateAssemblyResolver();

            var assemblyReadParams = new ReaderParameters
            {
                ReadSymbols = true,
                AssemblyResolver = assemblyResolver,
            };

            this.assemblyDefinition = AssemblyDefinition.ReadAssembly(dllStream, assemblyReadParams);
        }

        private void EnsureIoClose()
        {
            if (assemblyDefinition != null && assemblyDefinition.MainModule.SymbolReader != null)
            {
                assemblyDefinition.MainModule.SymbolReader.Dispose();
            }
            if (dllStream != null)
            {
                dllStream.Close();
            }
        }

        private void DeleteBackUp()
        {
            if (Directory.Exists(this.backeupDir))
            {
                Directory.Delete(this.backeupDir, true);
            }
        }

        private void WriteDll()
        {
            var writeParam = new WriterParameters
            {
                WriteSymbols = true,
            };

            assemblyDefinition.Write(this.dllPath, writeParam);
        }
    }
}