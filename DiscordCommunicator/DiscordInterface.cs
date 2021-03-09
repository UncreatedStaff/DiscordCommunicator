using Rocket.Core.Plugins;
using System;
using System.Collections;
using UnityEngine;
using static Rocket.Core.Logging.Logger;
using System.IO.MemoryMappedFiles;
using SDG.Unturned;
using CommsLib;

namespace DiscordCommunicator
{
    public class DiscordInterface : RocketPlugin<DiscordInterfaceConfig>
    {
        private IEnumerator timeout;
        private int timeoutCounter = 0;
        private IEnumerator coroutine;
        protected override void Load()
        {
            coroutine = WaitAndPrint(Configuration.Instance.RefreshRateSeconds);
            StartCoroutine(coroutine);
            Log("Started loop at game time: " + Time.time + " seconds. " +
                "Will refresh every " + Configuration.Instance.RefreshRateSeconds.ToString() + " seconds."
            );
        }
        protected override void Unload()
        {
            SendData(new FPlayerList(MMFInterface.ExtendArray(new string[] { "OFFLINE", "INTENTIONAL" }, MMFInterface.MaxPlayerCount), DateTime.Now));
            base.Unload();
        }
        private IEnumerator WaitAndPrint(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            SendData(GetSendData());
            coroutine = WaitAndPrint(Configuration.Instance.RefreshRateSeconds);
            StartCoroutine(coroutine);
        }
        private IEnumerator TimeoutWait(int WaitTime, int amount, MemoryMappedViewAccessor HandlerAccessor, MemoryMappedViewAccessor BotAccessor, byte[] send, MemoryMappedFile mmf, MemoryMappedFile handler)
        {
            yield return new WaitForSeconds(WaitTime / 1000f);
            byte d = HandlerAccessor.ReadByte(0);
            if (d != 1)
            {
                BotAccessor.WriteArray(0, send, 0, send.Length);
                timeoutCounter++;
                if (timeoutCounter > amount)
                {
                    timeoutCounter = 0;
                    BotAccessor.Dispose();
                    mmf.Dispose();
                    HandlerAccessor.Dispose();
                    handler.Dispose();
                    StopCoroutine(timeout);
                }
                else
                {
                    timeout = TimeoutWait(WaitTime, amount, HandlerAccessor, BotAccessor, send, mmf, handler);
                    StartCoroutine(timeout);
                }
            } else
            {
                timeoutCounter = 0;
                BotAccessor.Dispose();
                mmf.Dispose();
                HandlerAccessor.Dispose();
                handler.Dispose();
                StopCoroutine(timeout);
            }
        }
        private void SendData(FPlayerList fPlayerList)
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(this.Configuration.Instance.mmfName, MMFInterface.Length);
            MemoryMappedViewAccessor BotAccessor = mmf.CreateViewAccessor();
            MemoryMappedFile handler = MemoryMappedFile.CreateOrOpen(this.Configuration.Instance.mmfName + "-handler", 1);
            MemoryMappedViewAccessor HandlerAccessor = handler.CreateViewAccessor();
            byte[] send = new byte[MMFInterface.MaxPlayerCount];
            try
            {
                send = MMFInterface.EncodeMessage(fPlayerList);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
            }
            BotAccessor.WriteArray(0, send, 0, send.Length);
            timeout = TimeoutWait(100, 20, HandlerAccessor, BotAccessor, send, mmf, handler);
            StartCoroutine(timeout);
        }
        private FPlayerList GetSendData()
        {
            string[] players = new string[Provider.clients.Count];
            for(int i = 0; i < players.Length; i++)
            {
                players[i] = Provider.clients[i].playerID.nickName;
            }
            return new FPlayerList(MMFInterface.ExtendArray(players, MMFInterface.MaxPlayerCount), DateTime.Now);
        }
    }
}
