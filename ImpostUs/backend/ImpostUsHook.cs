using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostUs
{
    public static class ImpostUsHook
    {

        public static Memory.Mem mem = new Memory.Mem();
        public static ProcessMemory ProcessMemory = null;
        public static bool Hook()
        {
            var state = mem.OpenProcess("Among Us");

            if (state)
            {
                Methods.Init();
                Process proc = Process.GetProcessesByName("Among Us")[0];
                ProcessMemory = new ProcessMemory(proc);
                ProcessMemory.Open(ProcessAccess.AllAccess);
                return true;
            }
            return false;
        }

        private static ShipStatus prevShipStatus;
        private static ShipStatus shipStatus;
        static Dictionary<string, CancellationTokenSource> Tokens = new Dictionary<string, CancellationTokenSource>();
        static System.Action<uint> onChangeShipStatus;

        static void _ObserveShipStatus()
        {
            while (Tokens.ContainsKey("ObserveShipStatus") && Tokens["ObserveShipStatus"].IsCancellationRequested == false)
            {
                Thread.Sleep(250);
                shipStatus = ImpostUsHook.GetShipStatus();
                if (prevShipStatus.OwnerId != shipStatus.OwnerId)
                {
                    prevShipStatus = shipStatus;
                    onChangeShipStatus?.Invoke(shipStatus.Type);
                }
            }
        }

        public static void ObserveShipStatus(Action<uint> onChangeShipStatus)
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            if (Tokens.ContainsKey("ObserveShipStatus"))
            {
                Tokens["ObserveShipStatus"].Cancel();
                Tokens.Remove("ObserveShipStatus");
            }

            Tokens.Add("ObserveShipStatus", cts);
            ImpostUsHook.onChangeShipStatus = onChangeShipStatus;
            Task.Factory.StartNew(_ObserveShipStatus, cts.Token);
        }

        public static ShipStatus GetShipStatus()
        { 
            ShipStatus shipStatus = new ShipStatus();
            byte[] shipAob = ImpostUsHook.mem.ReadBytes(OffsetPattern.ShipStatusPointer, Utils.SizeOf<ShipStatus>());
            var aobStr = MakeAobString(shipAob, 4, "00 00 00 00 ?? ?? ?? ??");
            var aobResults = ImpostUsHook.mem.AoBScan(aobStr, true, true);
            aobResults.Wait();
            foreach (var result in aobResults.Result)
            {

                byte[] resultByte = ImpostUsHook.mem.ReadBytes(result.GetAddress(), Utils.SizeOf<ShipStatus>());
                ShipStatus resultInst = Utils.FromBytes<ShipStatus>(resultByte); 
                if (resultInst.AllVents != IntPtr.Zero && resultInst.NetId < uint.MaxValue - 10000)
                {
                    if (resultInst.MapScale < 6470545000000 && resultInst.MapScale > 0.1f)
                    {
                        shipStatus = resultInst;
                    }
                }
            }  
            return shipStatus;
        }

        private static Exception Exception(string v)
        {
            throw new NotImplementedException();
        }

        public static string MakeAobString(byte[] aobTarget, int length, string unknownText = "?? ?? ?? ??")
        {
            int cnt = 0;
            string aobData = "";
            foreach (var _byte in aobTarget)
            {
                if (_byte < 16)
                    aobData += "0" + _byte.ToString("X");
                else
                    aobData += _byte.ToString("X");

                if (cnt + 1 != 4)
                    aobData += " ";

                cnt++;
                if (cnt == length)
                {
                    aobData += $" {unknownText}";
                    break;
                }
            }
            return aobData;
        }
        public static List<PlayerData> GetAllPlayers()
        {
            List<PlayerData > datas = new List<PlayerData>();

            byte[] playerAoB = ImpostUsHook.mem.ReadBytes(OffsetPattern.PlayerControlPointer, Utils.SizeOf<PlayerControl>());
            string aobData = MakeAobString(playerAoB, 4, "?? ?? ?? ??"); 
            var result = ImpostUsHook.mem.AoBScan(aobData, true, true);
            result.Wait();
            var results = result.Result;
            foreach (var x in results)
            {
                var bytes = ImpostUsHook.mem.ReadBytes(x.GetAddress(), Utils.SizeOf<PlayerControl>());
                var PlayerControl = Utils.FromBytes<PlayerControl>(bytes);
                if (PlayerControl.SpawnFlags == 257 && PlayerControl.NetId < uint.MaxValue - 10000)
                {
                    datas.Add(new PlayerData()
                    {
                        Instance = PlayerControl,
                        PlayerControllPTROffset = x.GetAddress(),
                        PlayerControllPTR = new IntPtr((int)x)
                    });
                }
            }
            return datas;
        }
    }
}