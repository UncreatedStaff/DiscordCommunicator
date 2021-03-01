using Rocket.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCommunicator
{
    public class DiscordInterfaceConfig : IRocketPluginConfiguration
    {
        public float RefreshRateSeconds;
        public string mmfName;
        public void LoadDefaults()
        {
            RefreshRateSeconds = 60.0f;
            mmfName = "semirp";
        }
    }
}
