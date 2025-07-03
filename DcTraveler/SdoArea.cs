using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace DcTraveler
{
    public class SdoArea
    {
        //"Areaid":"1",
        public string Areaid { get; set; }
        //"AreaStat":1,
        public int AreaStat { get; set; }
        //"AreaOrder":4,
        public int AreaOrder { get; set; }
        //"AreaName":"陆行鸟",
        public string AreaName { get; set; }
        //"Areatype":1,
        public int Areatype { get; set; }
        //"AreaLobby":"ffxivlobby01.ff14.sdo.com",
        public string AreaLobby { get; set; }
        //"AreaGm":"ffxivgm01.ff14.sdo.com",
        public string AreaGm { get; set; }
        //"AreaPatch":"ffxivpatch01.ff14.sdo.com",
        public string AreaPatch { get; set; }
        //"AreaConfigUpload":"ffxivsdb01.ff14.sdo.com"
        public string AreaConfigUpload { get; set; }
    }
}
