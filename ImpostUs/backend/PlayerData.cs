using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ImpostUs
{
    public class PlayerData
    {
        #region ObserveStates
        private bool observe_dieFlag = false;
        #endregion

        public PlayerControl Instance;
        public System.Action<Vector2, byte> onDie;  

        private string PlayerInfoPTR = null;
        public IntPtr PlayerInfoPTROffset;

        public IntPtr PlayerControllPTR;
        public string PlayerControllPTROffset;


        Dictionary<string, CancellationTokenSource> Tokens = new Dictionary<string, CancellationTokenSource>();


        [Obsolete] 
        public void ObserveState()
        {
            if (PlayerInfo.HasValue)
            {
                if (observe_dieFlag == false && PlayerInfo.Value.IsDead == 1)
                {
                    observe_dieFlag = true;
                    onDie?.Invoke(Position, PlayerInfo.Value.ColorId);
                }
            }
        }
        
        public PlayerInfo? PlayerInfo
        {
            get
            {
                if (PlayerInfoPTROffset == IntPtr.Zero)
                {
                    var ptr =  Methods.Call_PlayerControl_GetData(this.PlayerControllPTR);
                    PlayerInfoPTR = ptr.GetAddress();
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(ImpostUsHook.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    PlayerInfoPTROffset = new IntPtr(ptr);
                    m_pInfo = pInfo;
                    return m_pInfo;
                }
                else
                {
                    PlayerInfo pInfo = Utils.FromBytes<PlayerInfo>(ImpostUsHook.mem.ReadBytes(PlayerInfoPTR, Utils.SizeOf<PlayerInfo>()));
                    m_pInfo = pInfo;
                    return m_pInfo;
                }

            }
        }

        private PlayerInfo? m_pInfo = null;

        public PlayerControl PlayerControl
        {
            get
            {
                var pcPtr = PlayerControllPTR;
                var pcBytes = ImpostUsHook.mem.ReadBytes(pcPtr.GetAddress(), Utils.SizeOf<PlayerControl>());
                var pc = Utils.FromBytes<PlayerControl>(pcBytes);
                return pc;
            }
        }

        public LightSource LightSource
        {
            get
            {
                var lsPtr = Instance.myLight;
                var lsBytes = ImpostUsHook.mem.ReadBytes(lsPtr.GetAddress(), Utils.SizeOf<LightSource>());
                var ls = Utils.FromBytes<LightSource>(lsBytes);
                return ls; 
            }
        }
        
        
        
        public void FreezeMemory_LightRange(float value)
        {
            var targetPointer = Utils.GetMemberPointer(Instance.myLight, typeof(LightSource), "LightRadius");
            ImpostUsHook.mem.FreezeValue(targetPointer.GetAddress(), "float", value.ToString("0.0"));
        }
        
        public void UnfreezeMemory_LightRange()
        {
            var targetPointer = Utils.GetMemberPointer(Instance.myLight, typeof(LightSource), "LightRadius");
            ImpostUsHook.mem.UnfreezeValue(targetPointer.GetAddress());
        }
        
        public PlayerPhysics PlayerPhysics
        {
            get
            {
                var ppPtr = Instance.MyPhysics;
                var ppBytes = ImpostUsHook.mem.ReadBytes(ppPtr.GetAddress(), Utils.SizeOf<PlayerPhysics>());
                var pp = Utils.FromBytes<PlayerPhysics>(ppBytes);
                return pp;
            }
        }
        
        public void WriteMemory_Speed(float value)
        {
            var targetPointer = Utils.GetMemberPointer(Instance.MyPhysics, typeof(PlayerPhysics), "Speed");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString("0.0"));
        }
        public void WriteMemory_ColorID(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "ColorId"); 
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }
        public void WriteMemory_Impostor(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "IsImpostor");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }
        
        public void WriteMemory_Dead(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "IsDead");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }


        /// <summary>
        /// Set Player Dead State.
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_IsDead(byte value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerInfoPTROffset, typeof(PlayerInfo), "IsDead");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "byte", value.ToString());
        }
        /// <summary>
        /// Set Player KillTimer
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_KillTimer(float value)
        {
            var targetPointer = Utils.GetMemberPointer(PlayerControllPTR, typeof(PlayerControl), "killTimer");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "float", value.ToString());
        }
        /// <summary>
        /// Set Player KillTimer
        /// </summary>
        /// <param name="value"></param>
        public void WriteMemory_SetNameTextColor(Color value)
        {
            var targetPointer = Utils.GetMemberPointer(Instance.nameText, typeof(TextRenderer), "Color");
            ImpostUsHook.mem.WriteMemory(targetPointer.GetAddress(), "float", value.r.ToString("0.0"));
            ImpostUsHook.mem.WriteMemory((targetPointer + 4).GetAddress(), "float", value.g.ToString("0.0"));
            ImpostUsHook.mem.WriteMemory((targetPointer + 8).GetAddress(), "float", value.b.ToString("0.0"));
            ImpostUsHook.mem.WriteMemory((targetPointer + 12).GetAddress(), "float", value.a.ToString("0.0"));
        }
         

        public void StopObserveState()
        {
            var key = Tokens.ContainsKey("ObserveState");
            if(key)
            {
                if (Tokens["ObserveState"].IsCancellationRequested == false)
                {
                    Tokens["ObserveState"].Cancel();
                    Tokens.Remove("ObserveState");
                }
            } 
        }
        public void StartObserveState()
        {
            if(Tokens.ContainsKey("ObserveState"))
            {
                Console.WriteLine("Already Observed!");
                return;
            }
            else
            {
                CancellationTokenSource cts = new CancellationTokenSource(); 
                Task.Factory.StartNew(() =>
                {
                    while (true)
                    {
                        if (PlayerInfo.HasValue)
                        {
                            if (observe_dieFlag == false && PlayerInfo.Value.IsDead == 1)
                            {
                                observe_dieFlag = true;
                                onDie?.Invoke(Position, PlayerInfo.Value.ColorId);
                            }
                        }
                        System.Threading.Thread.Sleep(25); 
                    }
                }, cts.Token);

                Tokens.Add("ObserveState", cts);
            }
          
        }

        public Vector2 Position
        {
            get
            {
                if (IsLocalPlayer)
                    return GetMyPosition();
                else
                    return GetSyncPosition();
            }
        }

        public void ReadMemory()
        {
            Instance = Utils.FromBytes<PlayerControl>(ImpostUsHook.mem.ReadBytes(PlayerControllPTROffset, Utils.SizeOf<PlayerControl>()));
        }

        public bool IsLocalPlayer
        {
            get => Instance.myLight != IntPtr.Zero;
        }

        public Vector2 GetSyncPosition()
        {
            try
            {
                int _offset_vec2_position = 60;
                int _offset_vec2_sizeOf = 8;
                var netTransform = ((int)Instance.NetTransform + _offset_vec2_position).ToString("X");
                var vec2Data= ImpostUsHook.mem.ReadBytes($"{netTransform}",_offset_vec2_sizeOf);
                if (vec2Data != null && vec2Data.Length != 0)
                {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return Vector2.Zero;
            }
        }
        public Vector2 GetMyPosition()
        {
            try
            {
                int _offset_vec2_position = 80;
                int _offset_vec2_sizeOf = 8;
                var netTransform = ((int)Instance.NetTransform + _offset_vec2_position).ToString("X");
                var vec2Data= ImpostUsHook.mem.ReadBytes($"{netTransform}",_offset_vec2_sizeOf);
                if (vec2Data != null && vec2Data.Length != 0)
                {
                    var vec2 = Utils.FromBytes<Vector2>(vec2Data);
                    return vec2;
                }
                else
                {
                    return Vector2.Zero;
                }
            }
            catch
            {
                return Vector2.Zero;
            }
        }
    }
}