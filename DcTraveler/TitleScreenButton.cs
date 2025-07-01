using Dalamud.Interface;
using Dalamud.Interface.Textures;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DcTraveler
{
    internal class TitleScreenButton : IDisposable
    {
        private IReadOnlyTitleScreenMenuEntry button;

        private readonly IDalamudPluginInterface pluginInterface;
        private readonly ITitleScreenMenu titleScreenMenu;
        private readonly ITextureProvider textureProvider;
        private readonly Plugin plugin;
        public TitleScreenButton(IDalamudPluginInterface pi, ITitleScreenMenu title, ITextureProvider textureProvider,Plugin plugin)
        {
            this.pluginInterface = pi;
            this.titleScreenMenu = title;
            this.textureProvider = textureProvider;
            this.plugin = plugin;
            pi.UiBuilder.Draw += AddEntry;
        }

        private void AddEntry()
        {
            var icon = this.textureProvider.GetFromFile(Path.Combine(pluginInterface.AssemblyLocation.DirectoryName!, "tsm.png"));
            this.button = this.titleScreenMenu.AddEntry("大区选择", icon, () => plugin.OpenDcSelectWindow());
            this.pluginInterface.UiBuilder.Draw -= AddEntry;
        }
        public void Dispose()
        {
            if (this.button != null)
            {
                titleScreenMenu.RemoveEntry(this.button);
            }
        }
    }
}
