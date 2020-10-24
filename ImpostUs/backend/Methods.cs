using System;
using System.Linq;

namespace ImpostUs
{
    public class CallAttribute : System.Attribute { }
    public class InitAttribute : System.Attribute { }

    public static class Methods
    {
        public static IntPtr PlayerControl_GetDataPTR = IntPtr.Zero;
        public static IntPtr PlayerTask_CompletePTR = IntPtr.Zero;

        [Init]
        public static void Init_PlayerControl_GetData()
        {
            Console.WriteLine("Init PlayerControl GetData");
            if (PlayerControl_GetDataPTR == IntPtr.Zero)
            {
                var aobScan = ImpostUsHook.mem.AoBScan(OffsetPattern.PlayerControlGetData);
                aobScan.Wait();
                if (aobScan.Result.Count() == 1)
                {
                    PlayerControl_GetDataPTR = (IntPtr)aobScan.Result.First(); 
                }
            }
        }

        [Call]
        public static int Call_PlayerControl_GetData(IntPtr playerControlPtr)
        { 
            if (PlayerControl_GetDataPTR != IntPtr.Zero)
            {
                var ptr = PlayerControl_GetDataPTR;
                var playerInfoAddress = ImpostUsHook.ProcessMemory.CallFunction(ptr, playerControlPtr); 
                return playerInfoAddress;
            }  
            return -1;
        }

        public static void Init()
        {
            var methods = typeof(Methods).GetMethods(); 
            foreach(var m in methods)
            {
                var atts = m.GetCustomAttributes(true); 
                foreach(var att in atts)
                { 
                    if (att.GetType() == typeof(InitAttribute))
                    { 
                        m.Invoke(null, null);
                    }
                }
            } 
        }
    }
}