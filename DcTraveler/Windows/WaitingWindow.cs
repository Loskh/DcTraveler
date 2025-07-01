using Dalamud.Interface.Windowing;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DcTraveler.Windows
{
    internal class WaitingWindow : Window, IDisposable
    {
        private DateTime openTime = DateTime.Now;
        public MigrationStatus Status = MigrationStatus.InPrepare;
        private Dictionary<MigrationStatus, string> statusText = new Dictionary<MigrationStatus, string>() {
            {MigrationStatus.Failed,"传送失败" },
            {MigrationStatus.InPrepare,"检查角色中..." },
            {MigrationStatus.InQueue,"排队中..." },
            {MigrationStatus.Completed,"传送完成" },
            {MigrationStatus.UnkownCompleted,"传送完成" },
        };
        public WaitingWindow() : base("WaitingOrder", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoSavedSettings)
        {
            //Position = ImGui.GetScrr
        }

        public override void PreDraw()
        {
            var viewport = ImGui.GetMainViewport();
            var center = viewport.GetCenter();
            ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
            //Log.Information("Middle");
            base.PreDraw();
        }
        public override void Draw()
        {
            Plugin.Font.Push();
            ImGui.Text("正在跨域传送中....");
            ImGui.Text($"已等待时间:{DateTime.Now - openTime}");
            ImGui.Text("目前状态:");
            ImGui.SameLine();
            ImGui.Text(this.statusText[this.Status]);
            Plugin.Font.Pop();
        }

        public void Open()
        {
            this.IsOpen = true;
            this.Status = MigrationStatus.InPrepare;
            this.openTime = DateTime.Now;
        }
        public void Dispose()
        {
        }
    }
}
