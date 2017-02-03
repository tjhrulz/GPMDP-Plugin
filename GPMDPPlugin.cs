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
            public musicInfo()
            {
                State = 0;
                Repeat = 0;
                Shuffle = 0;
                Volume = 0;
                Status = -1;
                DurationInms = 0;
                PositionInms = 0;
                Progress = 0;
                Rating = 0;

                Artist = "";
                Album = "";
                Title = "";
                Number = "";
                Year = "";
                Genre = "";
                Cover = "";
                CoverWebAddress = "";
                //File = "";
                Duration = "";
                Position = "";
                Lyrics = "";
            }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Title { get; set; }
            public string Number { get; set; }
            public string Year { get; set; }
            public string Genre { get; set; }
            public string Cover { get; set; }
            public string CoverWebAddress { get; set; }
            //public string File { get; set; }
            public string Duration { get; set; }
            public string Position { get; set; }
            public int Progress { get; set; }
            public int DurationInms { get; set; }
            public int PositionInms { get; set; }
            public int Rating { get; set; }
            public int Repeat { get; set; }
            public int Shuffle { get; set; }
            public int State { get; set; }
            public int Status { get; set; }
            public int Volume { get; set; }
            public string Lyrics { get; set; }
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
            Volume,
            Lyrics
        }

        //Info and player type of the measure
        private MeasureInfoType InfoType;

        //Locations of the most recent updated info
        //TODO DateTime does not track ms, this means that two things could update in the same second and now have a max.


        //Functions specific to GPMDP player
        private static musicInfo getGPMDPInfo()
        {
            return websocketInfoGPMDP;
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
            connect,
            repeat,
            shuffle,
            rating,
            lyrics
        }

        //Check if the GPMDP websocket is connected and if it is not connect
        private static void isGPMDPWebsocketConnected()
        {
            if (ws != null)
            {
                if (websocketInfoGPMDP.Status == -1 || websocketInfoGPMDP.Status == 0) { ws.ConnectAsync(); }
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
                    bool acceptedType = false;
                    
                    foreach (GPMInfoSupported currType in Enum.GetValues(typeof(GPMInfoSupported)))
                    {
                        if(currType.ToString().CompareTo(type.ToLower()) == 0)
                        {
                            acceptedType = true;
                        }
                    }

                    if (acceptedType)
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
                                        websocketInfoGPMDP.PositionInms = Convert.ToInt32(trackInfo.First);
                                        websocketInfoGPMDP.Progress = (int)((double)websocketInfoGPMDP.PositionInms / (double)websocketInfoGPMDP.DurationInms * 100);
                                        int trackSeconds = Convert.ToInt32(trackInfo.First.ToString()) / 1000;
                                        int trackMinutes = trackSeconds / 60;
                                        trackSeconds = trackSeconds % 60;

                                        websocketInfoGPMDP.Position = trackMinutes.ToString().PadLeft(2, '0') + ":" + trackSeconds.ToString().PadLeft(2, '0');
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("total") == 0)
                                    {
                                        websocketInfoGPMDP.DurationInms = Convert.ToInt32(trackInfo.First);
                                        websocketInfoGPMDP.Progress = (int)((double)websocketInfoGPMDP.PositionInms / (double)websocketInfoGPMDP.DurationInms * 100);
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
                                    websocketInfoGPMDP.Status = 1;
                                }
                                else
                                {
                                    API.Log(API.LogType.Warning, "New authorization code generated and saved:" + connectionInfo);
                                    sendGPMDPAuthCode(connectionInfo);
                                    WritePrivateProfileString("GPMDPPlugin", "AuthCode", connectionInfo, rainmeterFileSettingsLocation);
                                    authcode = connectionInfo;
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.repeat.ToString()) == 0 && acceptedVersion == true)
                            {
                                String repeatState = currentValue.ToString();
                                if (repeatState.ToUpper().CompareTo("NO_REPEAT") == 0)
                                {
                                    websocketInfoGPMDP.Repeat = 0;
                                }
                                else if (repeatState.ToUpper().CompareTo("SINGLE_REPEAT") == 0)
                                {
                                    websocketInfoGPMDP.Repeat = 1;
                                }
                                else if (repeatState.ToUpper().CompareTo("LIST_REPEAT") == 0)
                                {
                                    websocketInfoGPMDP.Repeat = 2;
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.shuffle.ToString()) == 0 && acceptedVersion == true)
                            {
                                String shuffleState = currentValue.ToString();
                                if (shuffleState.ToUpper().CompareTo("NO_SHUFFLE") == 0)
                                {
                                    websocketInfoGPMDP.Shuffle = 0;
                                }
                                else if (shuffleState.ToUpper().CompareTo("ALL_SHUFFLE") == 0)
                                {
                                    websocketInfoGPMDP.Shuffle = 1;
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.rating.ToString()) == 0 && acceptedVersion == true)
                            {
                                websocketInfoGPMDP.Rating = 0;
                                foreach (JProperty ratingInfo in currentValue)
                                {
                                    if (ratingInfo.Name.ToString().ToLower().CompareTo("liked") == 0)
                                    {
                                        if (Convert.ToBoolean(ratingInfo.First))
                                        {
                                            websocketInfoGPMDP.Rating = 1;
                                        }
                                    }
                                    else if (ratingInfo.Name.ToString().ToLower().CompareTo("disliked") == 0)
                                    {
                                        if (Convert.ToBoolean(ratingInfo.First))
                                        {
                                            websocketInfoGPMDP.Rating = -1;
                                        }
                                    }
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.lyrics.ToString()) == 0 && acceptedVersion == true)
                            {
                                websocketInfoGPMDP.Lyrics = currentValue.ToString();
                            }
                        }
                    }
                };


                ws.OnClose += (sender, d) => websocketInfoGPMDP.Status = 0;
                ws.OnOpen += (sender, d) =>
                {
                    websocketInfoGPMDP.Status = 1;
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
                ConnectionString += "\"arguments\": [\"Rainmeter GPMDP Plugin\"]\n";
                ConnectionString += "}";

                ws.SendAsync(ConnectionString, null);
            }
        }
        private static void getGPMDPAuthCode(String keycode)
        {
            String keycodeConnectionString = "{\n";
            keycodeConnectionString += "\"namespace\": \"connect\",\n";
            keycodeConnectionString += "\"method\": \"connect\",\n";
            keycodeConnectionString += "\"arguments\": [\"Rainmeter GPMDP Plugin\", \"" + keycode + "\"]\n";
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
                ConnectionString += "\"arguments\": [\"Rainmeter GPMDP Plugin\", \"" + authcode + "\"]\n";
                ConnectionString += "}";

                ws.SendAsync(ConnectionString, null);
                websocketInfoGPMDP.Status = 2;
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
        private static void GPMDPToggleShuffle()
        {
            String shuffleString = "{\n";
            shuffleString += "\"namespace\": \"playback\",\n";
            shuffleString += "\"method\": \"toggleShuffle\"\n";
            shuffleString += "}";
            ws.SendAsync(shuffleString, null);
        }
        private static void GPMDPPToggleRepeat()
        {
            String repeatString = "{\n";
            repeatString += "\"namespace\": \"playback\",\n";
            repeatString += "\"method\": \"toggleRepeat\"\n";
            repeatString += "}";
            ws.SendAsync(repeatString, null);
        }
        private static void GPMDPPSetPosition(int timeInms)
        {
            String repeatString = "{\n";
            repeatString += "\"namespace\": \"playback\",\n";
            repeatString += "\"method\": \"setCurrentTime\",\n";
            repeatString += "\"arguments\": [" + timeInms + "]\n";
            repeatString += "}";
            ws.SendAsync(repeatString, null);
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
                    maxValue = 100.0;
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
                    InfoType = MeasureInfoType.Status;
                    break;

                case "volume":
                    InfoType = MeasureInfoType.Volume;
                    break;

                case "lyrics":
                    InfoType = MeasureInfoType.Lyrics;
                    break;
                    
                default:
                    API.Log(API.LogType.Error, "GPMDPPlugin.dll: InfoType=" + infoType + " not valid, assuming title");
                    InfoType = MeasureInfoType.Title;
                    break;
            }

            //If not setup get the rainmeter settings file location and load the authcode
            if (rainmeterFileSettingsLocation.Length == 0)
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
            else if (a.Equals("repeat"))
            {
                GPMDPPToggleRepeat();
            }
            else if (a.Equals("shuffle"))
            {
                GPMDPToggleShuffle();
            }
            else if (a.Equals("play"))
            {
            }
            else if (a.Equals("pause"))
            {
            }
            else if (a.Contains("setposition"))
            {
                int percent = Convert.ToInt32(args.Substring(args.LastIndexOf(" ")));
                GPMDPPSetPosition(websocketInfoGPMDP.DurationInms * percent / 100);
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

            switch (InfoType)
            {
                case MeasureInfoType.State:
                    return websocketInfoGPMDP.State;
                case MeasureInfoType.Status:
                    return websocketInfoGPMDP.Status;
                case MeasureInfoType.Repeat:
                    return websocketInfoGPMDP.Repeat;
                case MeasureInfoType.Shuffle:
                    return websocketInfoGPMDP.Shuffle;
                case MeasureInfoType.Volume:
                    return websocketInfoGPMDP.Volume;
                case MeasureInfoType.Progress:
                    return websocketInfoGPMDP.Progress;
                case MeasureInfoType.Rating:
                    return websocketInfoGPMDP.Rating;
            }

            return 0.0;
        }

        internal string GetString()
        {
            switch (InfoType)
            {
                case MeasureInfoType.Artist:
                    return websocketInfoGPMDP.Artist;
                case MeasureInfoType.Album:
                    return websocketInfoGPMDP.Album;
                case MeasureInfoType.Title:
                    return websocketInfoGPMDP.Title;
                case MeasureInfoType.Number:
                    return websocketInfoGPMDP.Number;
                case MeasureInfoType.Year:
                    return websocketInfoGPMDP.Year;
                case MeasureInfoType.Genre:
                    return websocketInfoGPMDP.Genre;
                case MeasureInfoType.Cover:
                    return websocketInfoGPMDP.Cover;
                case MeasureInfoType.CoverWebAddress:
                    return websocketInfoGPMDP.CoverWebAddress;
                case MeasureInfoType.Duration:
                    return websocketInfoGPMDP.Duration;
                case MeasureInfoType.Position:
                    return websocketInfoGPMDP.Position;
                case MeasureInfoType.Lyrics:
                    return websocketInfoGPMDP.Lyrics;


                //These values are integers returned in update
                case MeasureInfoType.Repeat:
                    return null;
                case MeasureInfoType.Shuffle:
                    return null;
                case MeasureInfoType.Status:
                    return null;
                case MeasureInfoType.Volume:
                    return null;
                case MeasureInfoType.Progress:
                    return null;
                case MeasureInfoType.State:
                    return null;
                case MeasureInfoType.Rating:
                    return null;
            }
            return "";
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
