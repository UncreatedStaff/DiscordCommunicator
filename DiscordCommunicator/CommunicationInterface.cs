using Rocket.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordCommunicator
{

    public struct FPlayerList
    {
        public string[] _players;
        public DateTime timestamp;
        public string[] players
        {
            get
            {
                int i = 0;
                while (i < _players.Length)
                {
                    if (_players[i] == "")
                    {
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }
                string[] names = new string[i];
                for (int j = 0; j < names.Length; j++)
                {
                    names[j] = _players[j];
                }
                return names;
            }
            set
            {
                for (int i = 0; i < _players.Length; i++)
                {
                    if (value.Length > i)
                    {
                        _players[i] = value[i];
                    }
                    else
                    {
                        _players[i] = "";
                    }
                }
            }
        }
        public int PlayerCount { get { return players.Length; } }
        public FPlayerList(string[] players, DateTime timestamp)
        {
            this._players = players;
            this.timestamp = timestamp;
        }
        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            str.Append("Players:\n");
            for (int i = 0; i < this.players.Length; i++)
            {
                if (i == this.players.Length - 1)
                {
                    str.Append(this.players[i]);
                }
                else
                {
                    str.Append(this.players[i] + ", ");
                }
            }
            str.Append("\nData Sent at:\n");
            str.Append(this.timestamp.ToString());
            return str.ToString();
        }
    }

    public class CommunicationInterface
    {
        public const int DateTimeByteLength = 8;
        public const int MaxPlayerCount = 24;
        public const int MaxPlayerNameSize = 30;
        public const int BytesPerCharacter = 2;
        public const int END = 1;
        public const string NOT_HANDLED_MESSAGE = "Not Handled";
        public static byte[] not_valid
        {
            get
            {
                byte[] rtn = new byte[Length];
                rtn[0] = 2;
                return rtn;
            }
        }
        public static byte[] valid
        {
            get
            {
                byte[] rtn = new byte[Length];
                rtn[0] = 1;
                return rtn;
            }
        }
        public static int Length { get { return (MaxPlayerCount * MaxPlayerNameSize * BytesPerCharacter) + DateTimeByteLength + END; } }

        public static byte[] EncodeMessage(FPlayerList Message)
        {
            if (Message.players.Length > MaxPlayerCount)
            {
                Console.WriteLine("Players getting cut off, too many on list");
            }
            byte[] message = new byte[Length];
            byte[] timestamp = DateTimeToByteArray(Message.timestamp);
            for (int i = 0; i < timestamp.Length; i++)
            {
                message[i] = timestamp[i];
            }
            for (int i = 0; i < MaxPlayerCount; i++)
            {
                EncodeString(Message._players[i], MaxPlayerNameSize).CopyTo(message, DateTimeByteLength + (i * MaxPlayerNameSize * BytesPerCharacter));
            }
            message[Length - END] = 1;
            return message;
        }
        private static byte[] DateTimeToByteArray(DateTime dt)
        {
            long ticks = DateTime.Now.Ticks;
            byte[] bytes = BitConverter.GetBytes(ticks);
            return bytes;
        }
        private static byte[] EncodeString(string @string, byte NeededLength)
        {
            byte[] result = new byte[NeededLength * BytesPerCharacter];
            for (int i = 0; i < result.Length; i++)
            {
                result[i] = Encoding.Unicode.GetBytes(@string).ElementAtOrDefault(i); //Defaults to Byte(0)
            }
            return result;
        }
        public static FPlayerList DecodeMessage(byte[] bytes)
        {
            if (bytes[Length - END] != 1)
            {
                throw new Exception(NOT_HANDLED_MESSAGE);
            }
            string[] names = new string[MaxPlayerCount];
            for (int i = 0; i < MaxPlayerCount; i++)
            {
                byte[] name = new byte[MaxPlayerNameSize * BytesPerCharacter];
                for (int j = 0; j < name.Length; j++)
                {
                    name[j] = bytes[j + (i * MaxPlayerNameSize * BytesPerCharacter) + DateTimeByteLength];
                }
                names[i] = DecodeString(name);
            }
            return new FPlayerList(names, ReadTimeStamp(bytes));
        }
        private static string DecodeString(byte[] bytes)
        {
            string @string = "";
            for (int i = 0; i < bytes.Length; i += BytesPerCharacter)
            {
                if (bytes[i] != 0)
                {
                    byte[] array = new byte[BytesPerCharacter];
                    for (int j = 0; j < BytesPerCharacter; j++)
                        array[j] = bytes[i + j];
                    @string += Encoding.Unicode.GetChars(array)[0];
                }
            }
            return @string;
        }
        private static DateTime ReadTimeStamp(byte[] Bytes, int StartingIndex = 0)
        {
            byte[] timestampBytes = new byte[DateTimeByteLength];
            for (int i = 0; i < timestampBytes.Length; i++)
            {
                timestampBytes[i] = Bytes[i];
            }
            return ByteArrayToDateTime(timestampBytes);
        }
        private static DateTime ByteArrayToDateTime(byte[] dt)
        {
            long dti = BitConverter.ToInt64(dt, 0);
            DateTime dtt = DateTime.FromBinary(dti);
            return dtt;
        }

        public static string[] ExtendArray(string[] array, int WhatTo)
        {
            string[] newArray = new string[WhatTo];
            for(int i = 0; i < WhatTo; i ++)
            {
                if (i < array.Length)
                {
                    newArray[i] = array[i];
                } else
                {
                    newArray[i] = "";
                }
            }
            return newArray;
        }
    }
}
