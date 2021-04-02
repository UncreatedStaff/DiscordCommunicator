using Rocket.Core.Plugins;
using System;
using System.Collections;
using UnityEngine;
using static Rocket.Core.Logging.Logger;
using System.IO.MemoryMappedFiles;
using SDG.Unturned;
using CommsLib;
using MySql.Data.MySqlClient;
using Rocket.Unturned.Player;

namespace DiscordCommunicator
{
    public class DiscordInterface : RocketPlugin<DiscordInterfaceConfig>
    {
        private IEnumerator coroutine;
        private MySqlConnection SQL;
        private MMFInterface _interface;
        protected override void Load()
        {
            _interface = new MMFInterface(Configuration.Instance.MaxPlayerCount, Configuration.Instance.MaxNameLength);
            coroutine = WaitAndPrint(Configuration.Instance.RefreshRateSeconds);
            StartCoroutine(coroutine);
            Log("Started loop at game time: " + Time.time + " seconds. " +
                "Will refresh every " + Configuration.Instance.RefreshRateSeconds.ToString() + " seconds." + 
                (Configuration.Instance.LogWhenSendData ? " A log will be added everytime the playerlist is sent." : "")
            );
            if(Configuration.Instance.RemoveRankPrefix)
            {
                try
                {
                    SQL = new MySqlConnection(Configuration.Instance.SQLLogin.ConnectionString);
                }
                catch (MySqlException ex)
                {
                    Log("Couldn't connect to MySQL. Error:\n" + ex.ToString());
                }
                try
                {
                    Connect();
                }
                catch (MySqlException ex)
                {
                    Log(ex.ToString());
                }
            }
        }
        protected override void Unload()
        {
            SendData(new FPlayerList(MMFInterface.ExtendArray(new string[] { "OFFLINE", "INTENTIONAL" }, _interface.MaxPlayerCount), new byte[_interface.MaxPlayerCount], DateTime.Now));
            if(Configuration.Instance.RemoveRankPrefix)
            {
                try
                {
                    SQL.Close();
                }
                finally
                {
                    Log("Attempted to close MYSQL Connection");
                }
            }
            StopCoroutine(coroutine);
            base.Unload();
        }
        private IEnumerator WaitAndPrint(float waitTime)
        {
            yield return new WaitForSeconds(waitTime);
            SendData(GetSendData());
            coroutine = WaitAndPrint(Configuration.Instance.RefreshRateSeconds);
            StartCoroutine(coroutine);
        }
        private void SendData(FPlayerList fPlayerList)
        {
            MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(this.Configuration.Instance.mmfName, _interface.legacy ? _interface.LegacyLength : _interface.Length);
            MemoryMappedViewAccessor BotAccessor = mmf.CreateViewAccessor();
            byte[] send;
            try
            {
                send = _interface.EncodeMessage(fPlayerList);
                BotAccessor.WriteArray(0, send, 0, send.Length);
                if (Configuration.Instance.LogWhenSendData)
                    Log(send.Length.ToString() + " bytes of data sent over " + Configuration.Instance.mmfName);
            }
            catch (Exception ex)
            {
                Log(ex.ToString());
                Log("Data failed to send on " + Configuration.Instance.mmfName);
            }
        }
        private FPlayerList GetSendData()
        {
            string[] players = new string[Provider.clients.Count];
            byte[] teams = new byte[_interface.MaxPlayerCount];
            for(int i = 0; i < players.Length; i++)
            {
                players[i] = Configuration.Instance.RemoveRankPrefix ? RemovePrefix(Provider.clients[i]) : Provider.clients[i].playerID.characterName;
                teams[i] = (byte)(Provider.clients[i].player.quests.groupID.m_SteamID <= 255UL ? Provider.clients[i].player.quests.groupID.m_SteamID : 0);
            }
            return new FPlayerList(MMFInterface.ExtendArray(players, _interface.MaxPlayerCount), teams, DateTime.Now);
        }
        public static string RemoveOnce(string original, string toRemove)
        {
            int i = original.IndexOf(toRemove);
            if (i == -1) return original;
            else return original.Substring(0, i) + original.Substring(i + toRemove.Length);
        }
        public string RemovePrefix(SteamPlayer player)
        {
            string rtn;
            UncreatedLib.XPLevel xp = new UncreatedLib.XPLevel((int)GetXP(player.playerID.steamID.m_SteamID, player.player.quests.groupID.m_SteamID));
            if (player.player.quests.groupID.m_SteamID == 1UL)
            {
                rtn = RemoveOnce(player.playerID.nickName, $"[US-{xp.Abreviation}] ");
            }
            else if (player.player.quests.groupID.m_SteamID == 2UL)
            {
                rtn = RemoveOnce(player.playerID.nickName, $"[RU-{xp.Abreviation}] ");
            }
            else
            {
                rtn = RemoveOnce(player.playerID.nickName, $"[rec.] ");
            }
            return rtn;
        }
        public bool Connect()
        {
            try
            {
                SQL.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Log(ex.ToString());
                return false;
            }
        }
        public uint GetXP(ulong Steam64, ulong TeamID)
        {
            if (SQL.State != System.Data.ConnectionState.Open)
                Connect();

            uint xp = 0;
            using (MySqlCommand Q = new MySqlCommand($"SELECT `Balance` FROM `xp` WHERE `Steam64`='{Steam64}' AND `Team`='{TeamID}';", SQL))
            {
                using (MySqlDataReader R = Q.ExecuteReader())
                {
                    while (R.Read())
                    {
                        xp = R.GetUInt32("Balance");
                    }
                    R.Close();
                    R.Dispose();
                    Q.Dispose();
                }
            }
            return xp;
        }
    }
}
