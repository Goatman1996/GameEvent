using System.IO;
using Mono.Cecil;

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

        private void ReadDll()
        {
            this.dllStream = new FileStream(this.bakeDllPath, FileMode.Open);

            var assemblyResolver = InjecterUtil.CreateAssemblyResolver();

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