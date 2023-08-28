using System.IO;
using Mono.Cecil;

namespace GameEvent
{
    internal static class InjecterIo
    {
        internal static string BakeDll(string originDllPath)
        {
            var dllDir = Path.GetDirectoryName(originDllPath);
            var tempDir = dllDir + "/Temp";

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
            Directory.CreateDirectory(tempDir);

            var pdbPath = Path.ChangeExtension(originDllPath, ".pdb");
            var bakeDllPath = $"{tempDir}/bak.dll";
            var bakPdbPath = $"{tempDir}/bak.pdb";
            File.Copy(originDllPath, bakeDllPath);
            File.Copy(pdbPath, bakPdbPath);

            return bakeDllPath;
        }

        internal static void DeleteBake(string originDllPath)
        {
            var dllDir = Path.GetDirectoryName(originDllPath);
            var tempDir = dllDir + "/Temp";

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
            }
        }

        internal static FileStream CreateDllReadWriteStream(string dllPath)
        {
            FileStream stream = new FileStream(dllPath, FileMode.Open);
            return stream;
        }

        internal static AssemblyDefinition ReadAssembly(FileStream dllStream)
        {
            var assemblyResolver = InjecterUtil.CreateAssemblyResolver();

            var assemblyReadParams = new ReaderParameters
            {
                ReadSymbols = true,
                AssemblyResolver = assemblyResolver,
            };

            AssemblyDefinition assemblyDefinition = AssemblyDefinition.ReadAssembly(dllStream, assemblyReadParams);
            return assemblyDefinition;
        }

        internal static void WriteAssembly(string targetPath, AssemblyDefinition assemblyDefinition)
        {
            var writeParam = new WriterParameters
            {
                WriteSymbols = true,
            };

            assemblyDefinition.Write(targetPath, writeParam);
        }

        internal static void EnsureIoClose(AssemblyDefinition assemblyDefinition, FileStream dllStream)
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
    }
}