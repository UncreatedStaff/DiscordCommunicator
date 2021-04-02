using Rocket.API;
using System.Xml.Serialization;

namespace DiscordCommunicator
{
    public class DiscordInterfaceConfig : IRocketPluginConfiguration
    {
        public float RefreshRateSeconds;
        public string mmfName;
        [XmlElement("MySqlLoginData")]
        public SqlData SQLLogin;
        public bool RemoveRankPrefix;
        public bool LogWhenSendData;
        public int MaxPlayerCount;
        public int MaxNameLength;
        public void LoadDefaults()
        {
            RefreshRateSeconds = 60.0f;
            mmfName = "semirp";
            SQLLogin = new SqlData("localhost", "root", "password", "database", 3306);
            RemoveRankPrefix = true;
            LogWhenSendData = false;
            MaxPlayerCount = 24;
            MaxNameLength = 30;
        }
    }
    public struct SqlData
    {
        public string IP;
        public string Username;
        public string Password;
        public string Database;
        public ushort Port;
        public string ConnectionString { get { return $"server={IP};port={Port};uid={Username};pwd={Password};database={Database}"; } }

        public SqlData(string IP, string Username, string Password, string Database, ushort Port)
        {
            this.IP = IP;
            this.Username = Username;
            this.Password = Password;
            this.Database = Database;
            this.Port = Port;
        }
    }
}
