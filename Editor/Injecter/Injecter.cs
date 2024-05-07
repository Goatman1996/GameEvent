using System;
using System.IO;
using Mono.Cecil;

namespace GameEvent
{
    public partial class Injecter
    {
        public string DllPath { get => this.dllPath; }
        private string dllPath;
        private string dllNameNoExten;
        private string pdbPath;
        public Injecter(string dllPath)
        {
            this.dllPath = dllPath;
            this.dllNameNoExten = Path.GetFileNameWithoutExtension(this.dllPath);
            this.pdbPath = Path.ChangeExtension(this.dllPath, ".pdb");
        }

        public void PrepareIo()
        {
            this.logger.AppendLine($"[GameEvent] 开始注入");

            this.Initialize_IO();
            this.BackUpDll();
            this.ReadDll();
        }

        public bool hasInjected;

        public void CheckInjected()
        {
            this.hasInjected = this.HasInjected();
        }

        public void Write()
        {
            if (this.hasInjected == false)
            {
                this.WriteDll();
            }
        }

        public void EnsureClose()
        {
            this.EnsureIoClose();
        }
    }
}
