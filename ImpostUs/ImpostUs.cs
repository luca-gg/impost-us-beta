using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace ImpostUs
{
    static class ImpostUs
    {
        static volatile List<PlayerData> playerDatas = new List<PlayerData>();
        static bool wasVisible = false;

        [STAThread]
        static void Main()
        {
            if (!CurrentUser.IsAdministrator())
            {
                MessageBox.Show("ImpostUs needs administrator permissions!");
                return;
            }
            
            new Thread(() =>
            {
                Thread.Sleep(2000);
                
                RunOnUiThread(() => Controller.GUI.StatusText.Text = "Waiting for Process...");
                if (ImpostUsHook.Hook())
                {
                    RunOnUiThread(() => Controller.GUI.StatusText.Text = "Hooked");
                    ImpostUsHook.ObserveShipStatus(x =>
                    {
                        playerDatas.ForEach(playerData => playerData.StopObserveState());
                        playerDatas = ImpostUsHook.GetAllPlayers();
                        playerDatas.ForEach(playerData => playerData.StartObserveState());
                    });
                    new Thread(RunTick).Start();
                } 
                else
                {
                    RunOnUiThread(() => Controller.GUI.StatusText.Text = "Process not found! Restart to retry.");
                }
            }).Start();
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ImpostUsUi());
        }

        private static void RunOnUiThread(Action action) => Controller.GUI.Invoke(action);

        static void RunTick()
        {
            while (true)
            {
                RunOnUiThread(() => Controller.GUI.MainPanel.Visible = playerDatas.Count > 0);
                if (wasVisible && playerDatas.Count == 0)
                {
                    wasVisible = false;
                    RunOnUiThread(() => 
                    {
                        Controller.GUI.StatusText.Text = "Waiting to join a game...";
                        Controller.GUI.ImpostorCheckBox.Checked = false;
                    });
                }
                foreach (var data in playerDatas)
                {
                    if (data.IsLocalPlayer)
                    {
                        if (!wasVisible)
                        {
                            RunOnUiThread(() =>
                            {
                                Controller.GUI.StatusText.Text = "Ingame";
                                Controller.GUI.ImpostorCheckBox.Checked = data.PlayerInfo.Value.IsImpostor == 1;
                                Controller.Speed = data.PlayerPhysics.Speed;
                                Controller.GUI.SpeedTrackBar.Minimum = (int) Controller.Speed;
                                Controller.GUI.SpeedTrackBar.Maximum = (int) Controller.Speed + 10;
                                Controller.GUI.SpeedTrackBar.Value = (int) Controller.Speed;
                            });
                            wasVisible = true;
                        }
                        
                        data.WriteMemory_Impostor((byte) (Controller.Impostor ? 1 : 0));
                        data.WriteMemory_Speed(Controller.Speed);
                        
                        if (Controller.ResetKillTimer)
                        {
                            data.WriteMemory_KillTimer(0.00F);
                            Controller.ResetKillTimer = false;
                        }
                        if (Controller.AntiKillTimer)
                        {
                            data.WriteMemory_KillTimer(0.00F);
                        }
                    }

                    if (Controller.RevealRoles)
                    {
                        if (data.PlayerInfo.Value.IsImpostor == 1)
                        {

                            data.WriteMemory_SetNameTextColor(new Color(1, 0, 0, 1));
                        }
                        else
                        {
                            data.WriteMemory_SetNameTextColor(new Color(0, 1, 0, 1));
                        }
                    }
                    else
                    {
                        data.WriteMemory_SetNameTextColor(new Color(1, 1, 1, 1));
                    }
                }

                Thread.Sleep(100);
            }
        }
    }
}