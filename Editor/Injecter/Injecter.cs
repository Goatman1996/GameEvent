using System;
using System.IO;

namespace GameEvent
{
    public partial class Injecter
    {
        private string dllPath;
        private string pdbPath;
        public Injecter(string dllPath)
        {
            this.dllPath = dllPath;
            this.pdbPath = Path.ChangeExtension(this.dllPath, ".pdb");

            this.Initialize_IO();
        }

        public void Inject()
        {
            this.logger.AppendLine($"[GameEvent] 开始注入");

            this.BackUpDll();
            this.ReadDll();
            try
            {
                var injected = this.HasInjected();
                if (injected)
                {
                    this.EnsureIoClose();
                    this.DeleteBackUp();
                    return;
                }
                // 构建 事件 相关的使用 并 缓存
                this.BuildUsageCache();
                // 修改 事件
                this.ModifyGameEvent();
                // 创建 RegisterBridge
                this.InjectBridge();
                // 修改 事件 使用者
                this.ModifyUsage();



                this.WriteDll();

                this.logger.AppendLine($"[GameEvent] 注入完成");
                this.logger.Print();
            }
            catch (Exception e)
            {
                this.logger.PrintError();
                UnityEngine.Debug.LogException(e);
            }
            finally
            {
                this.EnsureIoClose();
                this.DeleteBackUp();
            }

        }
    }
}