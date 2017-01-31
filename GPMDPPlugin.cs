using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Rainmeter;
using System.IO;
using System.Net;
using System.Threading;

namespace GPMDPPlugin
{
    internal class Measure
    {
        //These Classes and enums are for use with every media player type, if you want to implement new info or a new player source then add it here
        class musicInfo
        {
            public musicInfo() { ConnectionStatus = 0; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Title { get; set; }
            public string Number { get; set; }
            public string Year { get; set; }
            public string Genre { get; set; }
            public string Cover { get; set; }
            public string CoverWebAddress { get; set; }
            public string File { get; set; }
            public string Duration { get; set; }
            public string Position { get; set; }
            public string Progress { get; set; }
            public string Rating { get; set; }
            public string Repeat { get; set; }
            public string Shuffle { get; set; }
            public int State { get; set; }
            public string Status { get; set; }
            public int ConnectionStatus { get; set; }
            public string Volume { get; set; }
            public DateTime LastUpdated { get; set; }
        }
        enum MeasureInfoType
        {
            Artist,
            Album,
            Title,
            Number,
            Year,
            Genre,
            Cover,
            CoverWebAddress,
            Duration,
            Position,
            Progress,
            Rating,
            Repeat,
            Shuffle,
            State,
            Status,
            ConnectionStatus,
            Volume
        }
        enum MeasurePlayerType
        {
            GPMDP,
            Soundnode,
            ChromeMusicInfo,
            Dynamic
        }
        
        //This array contains references to every players internal musicInfo
        private musicInfo[] musicInfoArray;

        //Info and player type of the measure
        private MeasureInfoType InfoType;
        //TODO Decide if I want to remove this as it is largely not needed as I dont know what the state is when I setup the array 
        //Likely I will keep it I just need then better handling of only doing the background tasks for that player type and testing of constructers and destructers as player type can change with DynamicVariables=1
        private MeasurePlayerType PlayerType;

        //Locations of the most recent updated info
        //TODO DateTime does not track ms, this means that two things could update in the same second and now have a max. 
        private static int mostRecentUpdateLoc = -1;
        private static DateTime mostRecentUpdateTime = DateTime.MinValue;

        //Called during init to setup musicInfoArray's references
        private void setupMusicInfoArray()
        {
            for (int i = 0; i < Enum.GetNames(typeof(MeasurePlayerType)).Length - 1; i++)
            {
                /*if (i == (int) MeasurePlayerType.AIMP)
                {
                    musicInfoArray[i] = getAIMPInfo();
                }
                else if (i == (int) MeasurePlayerType.CAD)
                {
                    musicInfoArray[i] = getCADInfo();
                }
                else if (i == (int) MeasurePlayerType.iTunes)
                {
                    musicInfoArray[i] = getiTunesInfo();
                }
                else if (i == (int) MeasurePlayerType.MediaMonkey)
                {
                    musicInfoArray[i] = getMediaMonkeyInfo();
                }
                else if (i == (int) MeasurePlayerType.Winamp)
                {
                    musicInfoArray[i] = getWinampInfo();
                }
                else if (i == (int) MeasurePlayerType.WMP)
                {
                    musicInfoArray[i] = getWMPInfo();
                }
                else if (i == (int) MeasurePlayerType.Spotify)
                {
                    musicInfoArray[i] = getSpotifyInfo();
                }
                else */
                if (i == (int)MeasurePlayerType.GPMDP)
                {
                    musicInfoArray[i] = getGPMDPInfo();
                }
                else if (i == (int)MeasurePlayerType.Soundnode)
                {
                    musicInfoArray[i] = getSoundnodeInfo();
                }
                else if (i == (int)MeasurePlayerType.ChromeMusicInfo)
                {
                    musicInfoArray[i] = getChromeMusicInfo();
                }
                else
                {
                    API.Log(API.LogType.Error, "Media player defined but not handled");
                }
            }
        }

        //Return the pointer to the internal musicInfo for each music source
        ////Functions specific to AIMP player
        //private static musicInfo getAIMPInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test AIMP Song", Artist = "Test AIMP Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getCADInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test CAD Song", Artist = "Test CAD Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getiTunesInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test iTunes Song", Artist = "Test iTunes Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getMediaMonkeyInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test MediaMonkey Song", Artist = "Test MediaMonkey Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getWinampInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test Winamp Song", Artist = "Test Winamp Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getWMPInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test WMP Song", Artist = "Test WMP Artist" };
        //
        //    return currInfo;
        //}
        //private static musicInfo getSpotifyInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test Spotify Song", Artist = "Test Spotify Artist" };
        //
        //    return currInfo;
        //}

        //Functions specific to GPMDP player
        private static musicInfo getGPMDPInfo()
        {
            return websocketInfoGPMDP;
        }
        private static musicInfo getSoundnodeInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test Soundnode Song", Artist = "Test Soundnode Artist" };

            //TODO implement, not 100% sure if/how I will do this as I can never test soundnode with the API always being limited and with sound node not having any sort of API

            return currInfo;
        }
        private static musicInfo getChromeMusicInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test ChromeMusicInfo Song", Artist = "Test ChromeMusicInfo Artist" };

            //TODO implement message passing from chrome to rainmeter, either using chrome native message passing or if that wont work then by opening a websocket.

            return currInfo;
        }

        //These variables, enums, and functions are all related to support for GPMDP
        public static WebSocket ws;
        private const String supportedAPIVersion = "1.1.0";
        private static musicInfo websocketInfoGPMDP = new musicInfo();
        private static string defaultCoverLocation;
        private static string coverOutputLocation;
        private static Thread GPMInitThread = new Thread(Measure.GPMDPWebsocketCreator);
        //private static int websocketState = 0;
        private static string authcode = "\0";
        private static string rainmeterFileSettingsLocation = "";
        private static bool sentInitialAuthcode = false;

        //The channel names that are handled in the OnMessage for the GPMDP websocket
        enum GPMInfoSupported
        {
            api_version,
            track,
            time,
            playstate,
            connect
        }

        //Check if the GPMDP websocket is connected and if it is not connect
        private static void isGPMDPWebsocketConnected()
        {
            if (ws != null)
            {
                if (websocketInfoGPMDP.ConnectionStatus == 0) { ws.ConnectAsync(); }
                else if (sentInitialAuthcode == false)
                {
                    sentInitialAuthcode = true;

                    if (authcode.Length > 30 && !authcode.Contains("\0"))
                    {
                        sendGPMDPAuthCode(authcode);
                    }
                    else
                    {
                        sendGPMDPRemoteRequest();
                    }
                }
            }
        }
        //Setup the websocket for GPMDP
        public static void GPMDPWebsocketCreator()
        {
            if (ws == null)
            {
                ws = new WebSocket("ws://localhost:5672");
                bool acceptedVersion = false;

                ws.OnMessage += (sender, d) =>
                {
                    String type = d.Data.Substring(12, d.Data.IndexOf(",") - 13);
                    GPMInfoSupported typeEnum;

                    if (Enum.TryParse(type.ToLower(), out typeEnum))
                    {
                        JObject data = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(d.Data);
                        JArray arrayData = new JArray(data);

                        foreach (JToken token in arrayData)
                        {
                            JToken currentProperty = token.First.Last;
                            JToken currentValue = token.Last.Last;

                            if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.api_version.ToString()) == 0)
                            {
                                String versionNumber = currentValue.ToString();

                                if (versionNumber.Substring(0, versionNumber.IndexOf(".")).CompareTo(supportedAPIVersion.Substring(0, versionNumber.IndexOf("."))) == 0)
                                {
                                    acceptedVersion = true;
                                }
                                else
                                {
                                    //TODO Have a rainmeter attribute flag to supress this error and attempt to continue working
                                    API.Log(API.LogType.Error, "GPMDP Websocket API version is: " + versionNumber + " this plugin was built for version " + supportedAPIVersion);
                                }

                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.time.ToString()) == 0 && acceptedVersion == true)
                            {
                                foreach (JProperty trackInfo in currentValue)
                                {
                                    if (trackInfo.Name.ToString().ToLower().CompareTo("current") == 0)
                                    {
                                        int trackSeconds = Convert.ToInt32(trackInfo.First.ToString()) / 1000;
                                        int trackMinutes = trackSeconds / 60;
                                        trackSeconds = trackSeconds % 60;

                                        websocketInfoGPMDP.Position = trackMinutes.ToString().PadLeft(2, '0') + ":" + trackSeconds.ToString().PadLeft(2, '0');
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("total") == 0)
                                    {
                                        int trackSeconds = Convert.ToInt32(trackInfo.First.ToString()) / 1000;
                                        int trackMinutes = trackSeconds / 60;
                                        trackSeconds = trackSeconds % 60;

                                        websocketInfoGPMDP.Duration = trackMinutes.ToString().PadLeft(2, '0') + ":" + trackSeconds.ToString().PadLeft(2, '0');
                                    }
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.track.ToString()) == 0 && acceptedVersion == true)
                            {
                                foreach (JProperty trackInfo in currentValue)
                                {
                                    if (trackInfo.Name.ToString().ToLower().CompareTo("title") == 0)
                                    {
                                        websocketInfoGPMDP.Title = trackInfo.First.ToString();
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("artist") == 0)
                                    {
                                        websocketInfoGPMDP.Artist = trackInfo.First.ToString();
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("album") == 0)
                                    {
                                        websocketInfoGPMDP.Album = trackInfo.First.ToString();
                                    }
                                    else if (trackInfo.First.ToString().Length != 0 && coverOutputLocation != null && trackInfo.Name.ToString().ToLower().CompareTo("albumart") == 0)
                                    {
                                        websocketInfoGPMDP.Cover = defaultCoverLocation;

                                        Thread t = new Thread(() => GetImageFromUrl(trackInfo.First.ToString(), coverOutputLocation));
                                        t.Start();
                                    }
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.playstate.ToString()) == 0 && acceptedVersion == true)
                            {
                                websocketInfoGPMDP.State = Convert.ToBoolean(currentValue) ? 1 : 2;
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.connect.ToString()) == 0)
                            {
                                String connectionInfo = currentValue.ToString();
                                if (connectionInfo.ToUpper().CompareTo("CODE_REQUIRED") == 0)
                                {
                                    API.Log(API.LogType.Warning, "Connection code bad, please send pin code using a bang as folows ''keycode ####''");
                                    websocketInfoGPMDP.ConnectionStatus = 1;
                                }
                                else
                                {
                                    API.Log(API.LogType.Warning, "New authorization code generated and saved:" + connectionInfo);
                                    sendGPMDPAuthCode(connectionInfo);
                                    WritePrivateProfileString("GPMDPPlugin", "AuthCode", connectionInfo, rainmeterFileSettingsLocation);
                                    authcode = connectionInfo;
                                }
                            }
                        }
                    }
                    websocketInfoGPMDP.LastUpdated = DateTime.UtcNow;
                };


                ws.OnClose += (sender, d) => websocketInfoGPMDP.ConnectionStatus = 0;
                ws.OnOpen += (sender, d) =>
                {
                    websocketInfoGPMDP.ConnectionStatus = 1;
                    if (authcode.Length > 30 && !authcode.Contains("\0"))
                    {
                        sentInitialAuthcode = true;
                        sendGPMDPAuthCode(authcode);
                    }
                };
                //ws.ConnectAsync();
            }
        }

        //These functions are related to elevating the GPMDP websocket to have remote status
        //Call sendGPMDPRemoteRequest to have GPMDP generate a 4 digit keycode, then getGPMDPAuthCode once you have recieved the code, and send authcode once GPMDP's websocket has sent you the perminate code
        private static void sendGPMDPRemoteRequest()
        {
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                String ConnectionString = "{\n";
                ConnectionString += "\"namespace\": \"connect\",\n";
                ConnectionString += "\"method\": \"connect\",\n";
                ConnectionString += "\"arguments\": [\"GPMDP API Tester\"]\n";
                ConnectionString += "}";

                ws.SendAsync(ConnectionString, null);
            }
        }
        private static void getGPMDPAuthCode(String keycode)
        {
            String keycodeConnectionString = "{\n";
            keycodeConnectionString += "\"namespace\": \"connect\",\n";
            keycodeConnectionString += "\"method\": \"connect\",\n";
            keycodeConnectionString += "\"arguments\": [\"GPMDP API Tester\", \"" + keycode + "\"]\n";
            keycodeConnectionString += "}";

            ws.SendAsync(keycodeConnectionString, null);
        }
        private static void sendGPMDPAuthCode(String authcode)
        {
            if (ws != null && ws.ReadyState == WebSocketState.Open)
            {
                String ConnectionString = "{\n";
                ConnectionString += "\"namespace\": \"connect\",\n";
                ConnectionString += "\"method\": \"connect\",\n";
                ConnectionString += "\"arguments\": [\"GPMDP API Tester\", \"" + authcode + "\"]\n";
                ConnectionString += "}";

                ws.SendAsync(ConnectionString, null);
                websocketInfoGPMDP.ConnectionStatus = 2;
            }
        }

        //These are functions that handle the sending of various GPMDP websocket commands
        //In theory if these were called before the websocket has been setup the could error but that would be impossible in rianmeter so adding the overhead for checks is unneeded.
        private static void GPMDPPlayPause()
        {
            String playPauseString = "{\n";
            playPauseString += "\"namespace\": \"playback\",\n";
            playPauseString += "\"method\": \"playPause\"\n";
            playPauseString += "}";
            ws.SendAsync(playPauseString, null);
        }
        private static void GPMDPForward()
        {
            String forwardString = "{\n";
            forwardString += "\"namespace\": \"playback\",\n";
            forwardString += "\"method\": \"forward\"\n";
            forwardString += "}";
            ws.SendAsync(forwardString, null);
        }
        private static void GPMDPPrevious()
        {
            String previousString = "{\n";
            previousString += "\"namespace\": \"playback\",\n";
            previousString += "\"method\": \"rewind\"\n";
            previousString += "}";
            ws.SendAsync(previousString, null);
        }

        //For downloading the image from the internet
        public static void GetImageFromUrl(string url, string filePath)
        {
            try
            {
                // Create http request
                HttpWebRequest httpWebRequest = (HttpWebRequest)HttpWebRequest.Create(url);
                using (HttpWebResponse httpWebReponse = (HttpWebResponse)httpWebRequest.GetResponse())
                {

                    // Read as stream
                    using (Stream stream = httpWebReponse.GetResponseStream())
                    {
                        Byte[] buffer = ReadStream(stream);
                        // Make sure the path folder exists
                        System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rainmeter/SpotifyPlugin");
                        // Write stream to file
                        File.WriteAllBytes(filePath, buffer);
                    }
                }
                // Change back to cover image
                //coverDownloaded = true;
                websocketInfoGPMDP.Cover = filePath;
                websocketInfoGPMDP.CoverWebAddress = url;
            }
            catch (Exception e)
            {
                API.Log(API.LogType.Error, "Unable to download album art to: " + coverOutputLocation);
                Console.WriteLine(e);
            }
        }
        private static byte[] ReadStream(Stream input)
        {
            byte[] buffer = new byte[1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }


        //To be used for reading and writing values from the rainmeter settings file
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        static extern int GetPrivateProfileString(string section, string key, string defaultValue,
            [In, Out] char[] value, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key,
            string value, string filePath);

        //Rainmeter functions
        internal Measure()
        {
            musicInfoArray = new musicInfo[Enum.GetNames(typeof(MeasurePlayerType)).Length];

            setupMusicInfoArray();

            if (GPMInitThread.ThreadState == ThreadState.Unstarted)
            {
                GPMInitThread.Start();
            }
        }

        internal virtual void Dispose()
        {
        }

        internal virtual void Reload(Rainmeter.API api, ref double maxValue)
        {
            string infoType = api.ReadString("PlayerInfo", "");
            switch (infoType.ToLowerInvariant())
            {
                case "artist":
                    InfoType = MeasureInfoType.Artist;
                    break;

                case "album":
                    InfoType = MeasureInfoType.Album;
                    break;

                case "title":
                    InfoType = MeasureInfoType.Title;
                    break;

                case "number":
                    InfoType = MeasureInfoType.Number;
                    break;

                case "year":
                    InfoType = MeasureInfoType.Year;
                    break;

                case "genre":
                    InfoType = MeasureInfoType.Genre;
                    break;

                case "cover":
                    InfoType = MeasureInfoType.Cover;
                    defaultCoverLocation = api.ReadPath("DefaultPath", "");
                    coverOutputLocation = api.ReadPath("CoverPath", "");
                    break;

                case "coverwebaddress":
                    InfoType = MeasureInfoType.CoverWebAddress;
                    break;

                case "duration":
                    InfoType = MeasureInfoType.Duration;
                    break;

                case "position":
                    InfoType = MeasureInfoType.Position;
                    break;

                case "progress":
                    InfoType = MeasureInfoType.Progress;
                    break;

                case "rating":
                    InfoType = MeasureInfoType.Rating;
                    break;

                case "repeat":
                    InfoType = MeasureInfoType.Repeat;
                    break;

                case "shuffle":
                    InfoType = MeasureInfoType.Shuffle;
                    break;

                case "state":
                    InfoType = MeasureInfoType.State;
                    break;

                case "status":
                    InfoType = MeasureInfoType.Status;
                    break;

                case "connectionstatus":
                    InfoType = MeasureInfoType.ConnectionStatus;
                    break;

                case "volume":
                    InfoType = MeasureInfoType.Volume;
                    break;

                default:
                    API.Log(API.LogType.Error, "GPMDPPlugin.dll: InfoType=" + infoType + " not valid, assuming title");
                    InfoType = MeasureInfoType.Title;
                    break;
            }

            string playerType = api.ReadString("PlayerType", "");
            switch (playerType.ToLowerInvariant())
            {
                case "dynamic":
                    PlayerType = MeasurePlayerType.Dynamic;
                    break;

                case "gpmdp":
                    PlayerType = MeasurePlayerType.GPMDP;
                    break;

                case "soundnode":
                    PlayerType = MeasurePlayerType.Soundnode;
                    break;

                case "ChromeMusicInfo":
                    PlayerType = MeasurePlayerType.ChromeMusicInfo;
                    break;

                default:
                    API.Log(API.LogType.Error, "GPMDPPlugin.dll: PlayerType=" + playerType + " not valid, assuming dynamic");
                    PlayerType = MeasurePlayerType.Dynamic;
                    break;
            }

            //If not setup get the rainmeter settings file location and load the authcode
            if(rainmeterFileSettingsLocation.Length == 0)
            {
                rainmeterFileSettingsLocation = api.GetSettingsFile();
                char[] authchar = new char[36];
                GetPrivateProfileString("GPMDPPlugin", "AuthCode", "", authchar, 37, rainmeterFileSettingsLocation);
                authcode = new String(authchar);
            }
        }

        internal void ExecuteBang(string args)
        {
            string a = args.ToLowerInvariant();
            if (a.Equals("playpause"))
            {
                GPMDPPlayPause();
            }
            else if (a.Equals("next"))
            {
                GPMDPForward();
            }
            else if (a.Equals("previous"))
            {
                GPMDPPrevious();
            }
            else if (a.Equals("play"))
            {
            }
            else if (a.Equals("pause"))
            {
            }
            else if (a.Contains("key"))
            {
                //Get the last 4 chars of the keycode, this should ensure that we always get it even when bang is a little off
                getGPMDPAuthCode(args.Substring(args.Length - 4, 4));
            }
            else
            {
                API.Log(API.LogType.Error, "GPMDPPlugin.dll: Invalid bang " + args);
            }
        }

        internal virtual double Update()
        {
            //TODO Make detection of reconnection more performant
            isGPMDPWebsocketConnected();

            if (PlayerType == MeasurePlayerType.Dynamic)
            {
                for (int i = 0; i < Enum.GetNames(typeof(MeasurePlayerType)).Length - 1; i++)
                {
                    if (musicInfoArray[i] != null && musicInfoArray[i].LastUpdated > mostRecentUpdateTime) { mostRecentUpdateLoc = i; }
                }
            }
            else
            {
                mostRecentUpdateLoc = (int)PlayerType;
            }

            switch (InfoType)
            {
                case MeasureInfoType.State:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].State;
                    }
                    return 0;
                case MeasureInfoType.ConnectionStatus:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].ConnectionStatus;
                    }
                    return 0;
            }

            return 0.0;
        }

        internal string GetString()
        {
            switch (InfoType)
            {
                case MeasureInfoType.Artist:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Artist;
                    }
                    return "";
            
                case MeasureInfoType.Album:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Album;
                    }
                    return "";
            
                case MeasureInfoType.Title:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Title;
                    }
                    return "";
            
                case MeasureInfoType.Number:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Number;
                    }
                    return "";
            
                case MeasureInfoType.Year:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Year;
                    }
                    return "";
            
                case MeasureInfoType.Genre:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Genre;
                    }
                    return "";

                case MeasureInfoType.Cover:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Cover;
                    }
                    return "";

                case MeasureInfoType.CoverWebAddress:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].CoverWebAddress;
                    }
                    return "";

                case MeasureInfoType.Duration:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Duration;
                    }
                    return "";

                case MeasureInfoType.Position:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Position;
                    }
                    return "";

                case MeasureInfoType.Progress:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Progress;
                    }
                    return "";
            
                case MeasureInfoType.Rating:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Rating;
                    }
                    return "";
            
                case MeasureInfoType.Repeat:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Repeat;
                    }
                    return "";
            
                case MeasureInfoType.Shuffle:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Shuffle;
                    }
                    return "";
            
                case MeasureInfoType.Status:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Status;
                    }
                    return "";
            
                case MeasureInfoType.Volume:
                    if (mostRecentUpdateLoc >= 0)
                    {
                        return musicInfoArray[mostRecentUpdateLoc].Volume;
                    }
                    return "";
            }
            return null;
        }


    }
    public static class Plugin
    {
        static IntPtr StringBuffer = IntPtr.Zero;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            Measure.ws.CloseAsync();
            GCHandle.FromIntPtr(data).Free();

            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            return measure.Update();
        }

        [DllExport]
        public static IntPtr GetString(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            if (StringBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(StringBuffer);
                StringBuffer = IntPtr.Zero;
            }

            string stringValue = measure.GetString();
            if (stringValue != null)
            {
                StringBuffer = Marshal.StringToHGlobalUni(stringValue);
            }

            return StringBuffer;
        }
        [DllExport]
        public static void ExecuteBang(IntPtr data, IntPtr args)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.ExecuteBang(Marshal.PtrToStringUni(args));
        }
    }
}
