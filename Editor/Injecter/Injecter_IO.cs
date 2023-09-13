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
            this.backeupDir = backUpTempDirName;

            this.bakeDllPath = $"{this.backeupDir}/bak_{this.dllNameNoExten}.dll";
            this.bakPdbPath = $"{this.backeupDir}/bak_{this.dllNameNoExten}.pdb";
        }

        public const String backUpTempDirName = "./Temp/GameEvent";
        public static void DoBackUpDirCreateOneTime(string targetDir)
        {
            var backeupDir = backUpTempDirName;
            if (Directory.Exists(backeupDir))
            {
                Directory.Delete(backeupDir, true);
            }
            Directory.CreateDirectory(backeupDir);
            foreach (var file in Directory.GetFiles(targetDir))
            {
                var destFileName = Path.GetFileName(file);
                File.Copy(file, backeupDir + $"/{destFileName}");
            }
        }

        public static void DeleteBackUpOneTime(string targetDir)
        {
            // var backeupDir = backUpTempDirName;
            // if (Directory.Exists(backeupDir))
            // {
            //     Directory.Delete(backeupDir, true);
            // }
        }


        private void BackUpDll()
        {
            File.Copy(this.dllPath, this.bakeDllPath);
            File.Copy(this.pdbPath, this.bakPdbPath);
        }

        private DefaultAssemblyResolver CreateAssemblyResolver()
        {
            var fullPath = Path.GetFullPath(this.dllPath);
            var fullPathDir = Path.GetDirectoryName(fullPath);

            HashSet<string> searchDir = new HashSet<string>();
            foreach (var path in (from asm in AppDomain.CurrentDomain.GetAssemblies()
                                  select Path.GetDirectoryName(asm.ManifestModule.FullyQualifiedName)).Distinct())
            {
                try
                {
                    string targetPath = path;

                    if (targetPath == fullPathDir)
                    {
                        targetPath = backUpTempDirName;
                    }
                    if (searchDir.Contains(targetPath) == false)
                    {
                        // UnityEngine.Debug.Log(targetPath);
                        searchDir.Add(targetPath);
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
            this.dllStream = new FileStream(this.bakeDllPath, FileMode.Open, FileAccess.ReadWrite);

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
                assemblyDefinition.Dispose();
            }
            if (dllStream != null)
            {
                dllStream.Close();
                dllStream = null;
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