using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DcTraveler.Windows
{
    internal class WorldSelectorWindows : Window, IDisposable
    {
        public WorldSelectorWindows() : base("超域传送", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize)
        {
        }

        private bool showSourceWorld = true;
        private bool showTargetWorld = true;
        private bool isBack = false;
        private int currentDcIndex = 0;
        private int currentWorldIndex = 0;
        private string[] dc = new string[0];
        private List<string[]> world = new();
        private int targetDcIndex = 0;
        private int targetWorldIndex = 0;
        public override void Draw()
        {
            if (showSourceWorld)
            {
                ImGui.BeginTable("##TableCurrent", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBodyUntilResize);
                ImGui.TableSetupColumn("当前大区", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("当前服务器", ImGuiTableColumnFlags.WidthFixed, 300);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.ListBox("##CurrentDc", ref currentDcIndex, dc, dc.Length, 4);
                ImGui.TableNextColumn();
                ImGui.ListBox("##CurrentServer", ref currentWorldIndex, world[currentDcIndex], world[targetDcIndex].Length, 7);
                ImGui.EndTable();
            }

            if (showTargetWorld)
            {
                ImGui.BeginTable("##TableCurrent", 2, ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.NoBordersInBodyUntilResize);
                ImGui.TableSetupColumn("目标大区", ImGuiTableColumnFlags.WidthFixed, 100);
                ImGui.TableSetupColumn("目标服务器", ImGuiTableColumnFlags.WidthFixed, 300);
                ImGui.TableHeadersRow();

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.ListBox("##TargettDc", ref targetDcIndex, dc, dc.Length, 4);
                ImGui.TableNextColumn();
                ImGui.ListBox("##TargetServer", ref targetWorldIndex, world[targetDcIndex], world[targetDcIndex].Length, 7);
                ImGui.EndTable();
            }
            ImGui.Button(isBack ? "返回" : "传送");
            ImGui.SameLine();
            ImGui.Button("取消");
        }
        public void OpenTravelWindow(bool showSourceWorld, bool showTargetWorld, bool isBack, List<Area> areas, string currentDcName = null, string currentWorldCode = null, string targetDcName = null, string targetWorldCode = null)
        {
            this.showSourceWorld = showSourceWorld;
            this.showTargetWorld = showTargetWorld;
            this.isBack = isBack;
            this.dc = new string[areas.Count];
            for (int i = 0; i < areas.Count; i++)
            {
                this.dc[i] = areas[i].AreaName;
                this.world.Add(new string[areas[i].GroupList.Count]);
                if (currentDcName == areas[i].AreaName)
                    this.currentDcIndex = i;
                else if (targetDcName == areas[i].AreaName)
                    this.targetDcIndex = i;
                for (int j = 0; j < areas[i].GroupList.Count; j++)
                {
                    if (currentDcName == areas[i].AreaName && areas[i].GroupList[j].GroupCode == currentWorldCode)
                        this.currentWorldIndex = j;
                    else if (targetDcName == areas[i].AreaName && areas[i].GroupList[j].GroupCode == targetWorldCode)
                        this.targetWorldIndex = j;
                    this.world[i][j] = areas[i].GroupList[j].GroupName;
                }
            }
            this.IsOpen = true;
        }

        public void Dispose()
        {
        }
    }
}
