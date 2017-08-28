using System;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Rainmeter;
using System.IO;
using System.Net;
using System.Threading;
using System.Diagnostics;
using System.Collections.Generic;

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
                Volume = 100;
                Status = -1;
                DurationInms = 0;
                PositionInms = 0;
                Progress = 0;
                Rating = 0;

                Artist = "";
                Album = "";
                Title = "";
                //Number = "";
                //Year = "";
                //Genre = "";
                Cover = null;
                CoverWebAddress = "";
                //File = "";
                //Position and duration now calculated at update time so it respects DisableLeadingZero
                //Duration = "00:00";
                //Position = "00:00";
                Lyrics = "";

                //ThemeType = 0;
                //ThemeColor = "222, 79, 44";

                Queue = new List<JToken>();

                //This loop populates the websocket queue with null
                JObject blankInfo = new JObject();

                foreach (QueueInfoType type in Enum.GetValues(typeof(QueueInfoType)))
                {
                    JProperty info = new JProperty(type.ToString(), "");

                    if (type == QueueInfoType.Duration || type == QueueInfoType.PlayCount || type == QueueInfoType.Index)
                    {
                        info = new JProperty(type.ToString(), "0");
                    }
                    blankInfo.Add(info);
                }
                Queue.Add(blankInfo);
            }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Title { get; set; }
            //public string Number { get; set; }
            //public string Year { get; set; }
            //public string Genre { get; set; }
            public string Cover { get; set; }
            public string CoverWebAddress { get; set; }
            //public string File { get; set; }
            //public string Duration { get; set; }
            //public string Position { get; set; }
            public double Progress { get; set; }
            public int DurationInms { get; set; }
            public int PositionInms { get; set; }
            public int Rating { get; set; }
            public int Repeat { get; set; }
            public int Shuffle { get; set; }
            public int State { get; set; }
            public int Status { get; set; }
            public int Volume { get; set; }
            public string Lyrics { get; set; }
            //public int ThemeType { get; set; }
            //public string ThemeColor { get; set; }
            public List<JToken> Queue { get; set; }
        }

        //Possible info types the measure can be
        enum MeasureInfoType
        {
            Artist,
            Album,
            Title,
            //Number,
            //Year,
            //Genre,
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
            Lyrics,
            ThemeType,
            ThemeColor,
            Queue
        }
        //Info type of the current measure
        private MeasureInfoType InfoType;

        //Possible infotypes queue info can be
        enum QueueInfoType
        {
            Artist = 0,
            Album = 1,
            Title = 2,
            AlbumArt = 3,
            Duration = 4,
            PlayCount = 5,
            Index = 6,
            ID = 7,
            AlbumArtist = 8,
            AlbumID = 9,
            ArtistID = 10,
            ArtistImage = 11,
        }
        //Queue info type and location to read it from for current measure
        private int myQueueLocationToRead = 0;
        private QueueInfoType myQueueInfoType = QueueInfoType.Title;

        //These variables, enums, and functions are all related to support for GPMDP
        public static WebSocket ws;
        //Version the plugin was built around, should be no breaking issues until version 2.x.x
        private const String supportedAPIVersion = "1.1.0";

        //Latest info from the websocket
        private static musicInfo websocketInfoGPMDP = new musicInfo();
        //Not stored in websocket so each measure can have their own, if normal cover is null it is replaced with this later on
        private string defaultCoverLocation = "";
        //Fallback location to download coverart to
        private static string coverOutputLocation = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rainmeter/GPMDPPlugin/cover.png";
        //Default theme state and color for GPMDP
        private static int lastKnownThemeType = 0;
        private static string lastKnownThemeColor = "222, 79, 44";

        //Flags related to Prgoress, Position, and Duration
        private int asDecimal = 0;
        private int disableLeadingZero = 0;
        private int includeMS = 0;

        //For setting queue locations relatively/finding current song faster
        private static int lastKnownQueueLocation = 0;
        //Threads related to updating queue
        private static Thread queueUpdateThread;
        private static Thread queueLocUpdateThread;

        //For reading and setting authcode, flow is a little different now that it is read from GPMDP settings file but the old code is still in just in case
        private static string authcode = "\0";
        private static string rainmeterFileSettingsLocation = "";
        private static bool sentInitialAuthcode = false;

        //So networking never happens on UI thread the creation and reconnection of websockets are handled on a different thread
        private static Thread GPMInitThread = new Thread(Measure.GPMDPWebsocketCreator);
        private static Thread GPMReconnectThread = new Thread(Measure.isGPMDPWebsocketConnected);

        //Store how long its been since last attempt
        private static int GPMReconnectTimer;
        //Time between reconnect attempts in ms
        private const int timeBetweenReconnectAttempts = 1000;

        //The channel names that are handled in the OnMessage for the GPMDP websocket, to add a new one add it here first
        //Theme and results are not in this due to the way they are handled
        enum GPMInfoSupported
        {
            //Note this list does not include 2 theme support channels
            api_version,
            track,
            time,
            playstate,
            connect,
            repeat,
            shuffle,
            rating,
            lyrics,
            volume,
            queue
        }

        //Check if the GPMDP websocket is connected and if it is not connect
        private static void isGPMDPWebsocketConnected()
        {
            if (ws != null)
            {
                if (websocketInfoGPMDP.Status == -1 || websocketInfoGPMDP.Status == 0)
                {
                    int currentTicks = Environment.TickCount;
                    if (currentTicks > GPMReconnectTimer + timeBetweenReconnectAttempts || (currentTicks < 0 && GPMReconnectTimer > 0))
                    {
                        GPMReconnectTimer = currentTicks;
                        if (Process.GetProcessesByName("Google Play Music Desktop Player").Length > 0)
                        {
                            ws.Connect();
                        }
                    }
                }
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
        //Setup the websocket for GPMDP, you can find code relating to updating music info in the onMessage here
        public static void GPMDPWebsocketCreator()
        {
            if (ws == null)
            {
                ws = new WebSocket("ws://localhost:5672");
                bool acceptedVersion = false;

                ws.OnMessage += (sender, d) =>
                {
                    //Get the location of what type of info this is, which is formatted as :"%%%%%%",
                    String type = d.Data.Substring(d.Data.IndexOf(":") + 2, d.Data.IndexOf(",") - d.Data.IndexOf(":") - 3);
                    bool acceptedType = false;
                    //API.Log(API.LogType.Notice, "type:" + type);

                    foreach (GPMInfoSupported currType in Enum.GetValues(typeof(GPMInfoSupported)))
                    {
                        if (currType.ToString().CompareTo(type.ToLower()) == 0)
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
                                        try
                                        {
                                            websocketInfoGPMDP.PositionInms = Convert.ToInt32(trackInfo.First);
                                            if (websocketInfoGPMDP.DurationInms != 0)
                                            {
                                                websocketInfoGPMDP.Progress = ((double)websocketInfoGPMDP.PositionInms / (double)websocketInfoGPMDP.DurationInms * 100);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            API.Log(API.LogType.Error, "Unable to convert the position from GPMDP, report this issue on the GPMDP plugin github page");
                                            API.Log(API.LogType.Debug, e.ToString());
                                        }
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("total") == 0)
                                    {
                                        try
                                        {
                                            websocketInfoGPMDP.DurationInms = Convert.ToInt32(trackInfo.First);
                                            if (websocketInfoGPMDP.DurationInms != 0)
                                            {
                                                websocketInfoGPMDP.Progress = ((double)websocketInfoGPMDP.PositionInms / (double)websocketInfoGPMDP.DurationInms * 100);
                                            }
                                        }
                                        catch (Exception e)
                                        {
                                            API.Log(API.LogType.Error, "Unable to convert the duration from GPMDP, report this issue on the GPMDP plugin github page");
                                            API.Log(API.LogType.Debug, e.ToString());
                                        }
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
                                        if (trackInfo.First.ToString().ToLower().CompareTo("unknown artist") != 0)
                                        {
                                            websocketInfoGPMDP.Artist = trackInfo.First.ToString();
                                        }
                                        else
                                        {
                                            websocketInfoGPMDP.Artist = "";
                                        }
                                    }
                                    else if (trackInfo.Name.ToString().ToLower().CompareTo("album") == 0)
                                    {
                                        if (trackInfo.First.ToString().ToLower().CompareTo("unknown album") != 0)
                                        {
                                            websocketInfoGPMDP.Album = trackInfo.First.ToString();
                                        }
                                        else
                                        {
                                            websocketInfoGPMDP.Album = "";
                                        }
                                    }
                                    else if (trackInfo.First.ToString().Length != 0 && coverOutputLocation != null && trackInfo.Name.ToString().ToLower().CompareTo("albumart") == 0)
                                    {
                                        if (!trackInfo.First.ToString().Contains("default"))
                                        {
                                            websocketInfoGPMDP.Cover = null;
                                            Thread t = new Thread(() => GetImageFromUrl(trackInfo.First.ToString(), coverOutputLocation));
                                            t.Start();
                                        }
                                        else
                                        {
                                            websocketInfoGPMDP.Cover = null;
                                            websocketInfoGPMDP.CoverWebAddress = trackInfo.First.ToString();
                                        }
                                    }
                                }

                                if (queueLocUpdateThread != null)
                                {
                                    queueLocUpdateThread.Abort();
                                    queueLocUpdateThread.Join();
                                }
                                queueLocUpdateThread = new Thread(() => updateQueueLoc());
                                queueLocUpdateThread.Start();
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.playstate.ToString()) == 0 && acceptedVersion == true)
                            {
                                try
                                {
                                    websocketInfoGPMDP.State = Convert.ToBoolean(currentValue) ? 1 : 2;
                                }
                                catch (Exception e)
                                {
                                    API.Log(API.LogType.Error, "Unable to convert the play state from GPMDP, report this issue on the GPMDP plugin github page");
                                    API.Log(API.LogType.Debug, e.ToString());
                                }
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
                                int internalRating = 0;
                                try
                                {
                                    foreach (JProperty ratingInfo in currentValue)
                                    {
                                        if (ratingInfo.Name.ToString().ToLower().CompareTo("liked") == 0)
                                        {
                                            if (Convert.ToBoolean(ratingInfo.First))
                                            {
                                                internalRating = 1;
                                            }
                                        }
                                        else if (ratingInfo.Name.ToString().ToLower().CompareTo("disliked") == 0)
                                        {
                                            if (Convert.ToBoolean(ratingInfo.First))
                                            {
                                                internalRating = -1;
                                            }
                                        }
                                    }
                                }
                                catch (Exception e)
                                {
                                    API.Log(API.LogType.Error, "Unable to convert the song rating from GPMDP, report this issue on the GPMDP plugin github page");
                                    API.Log(API.LogType.Debug, e.ToString());
                                }
                                websocketInfoGPMDP.Rating = internalRating;
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.lyrics.ToString()) == 0 && acceptedVersion == true)
                            {
                                websocketInfoGPMDP.Lyrics = currentValue.ToString();
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.volume.ToString()) == 0 && acceptedVersion == true)
                            {
                                try
                                {
                                    websocketInfoGPMDP.Volume = Convert.ToInt16(currentValue);
                                }
                                catch (Exception e)
                                {
                                    API.Log(API.LogType.Error, "Unable to convert the volume from GPMDP, report this issue on the GPMDP plugin github page");
                                    API.Log(API.LogType.Debug, e.ToString());
                                }
                            }
                            else if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.queue.ToString()) == 0 && acceptedVersion == true)
                            {
                                //TODO Better handle both this and album art downloader being run again before the last one has finished
                                if (queueUpdateThread != null)
                                {
                                    queueUpdateThread.Abort();
                                    queueUpdateThread.Join();
                                }
                                queueUpdateThread = new Thread(() => updateQueueInfo(currentValue));
                                queueUpdateThread.Start();
                            }
                        }
                    }
                    else if (type.CompareTo("result") == 0 && acceptedVersion == true)
                    {
                        //JObject data = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(d.Data);
                        //JArray arrayData = new JArray(data);
                        //
                        //if (data.First.Next.ToString() == "\"requestID\": 1")
                        //{
                        //    foreach (JToken innerData in arrayData)
                        //    {
                        //        foreach (JToken info in innerData)
                        //        {
                        //            if (info.ToString().Contains("return"))
                        //            {
                        //                foreach (JToken songInfo in info.Next.Children())
                        //                {
                        //                    lastKnownQueueLocation = (int)(songInfo["index"]) - 1;
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                    }
                    else if (type.Contains("theme"))
                    {
                        JObject data = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(d.Data);
                        JArray arrayData = new JArray(data);
                        //API.Log(API.LogType.Notice, "data:" + data);
                        foreach (JToken token in arrayData)
                        {
                            JToken currentProperty = token.First.Last;
                            JToken currentValue = token.Last.Last;

                            if (currentProperty.ToString().ToLower().Contains("themetype") && acceptedVersion == true)
                            {
                                if (currentValue.ToString().ToUpper().CompareTo("FULL") == 0)
                                {
                                    lastKnownThemeType = 1;
                                }
                                else
                                {
                                    lastKnownThemeType = 0;
                                }
                            }
                            else if (currentProperty.ToString().ToLower().Contains("themecolor") && acceptedVersion == true)
                            {
                                String rgbString = currentValue.ToString();

                                try
                                {
                                    if (rgbString.ToLower().Contains("rgb"))
                                    {
                                        lastKnownThemeColor = rgbString.Substring(rgbString.IndexOf("(") + 1, rgbString.IndexOf(")") - rgbString.IndexOf("(") - 1);
                                    }
                                    else
                                    {
                                        //Cutoff the # that is at the beginning
                                        rgbString = rgbString.Substring(1);
                                        string r = Convert.ToInt32(rgbString.Substring(0, 2), 16).ToString();
                                        string g = Convert.ToInt32(rgbString.Substring(2, 2), 16).ToString();
                                        string b = Convert.ToInt32(rgbString.Substring(4, 2), 16).ToString();

                                        lastKnownThemeColor = r + ", " + g + ", " + b;
                                    }
                                }
                                catch (Exception e)
                                {
                                    API.Log(API.LogType.Error, "Unable to convert the theme color from GPMDP, report this issue on the GPMDP plugin github page");
                                    API.Log(API.LogType.Debug, e.ToString());
                                }
                            }
                        }
                    }
                };


                ws.OnClose += (sender, d) =>
                {
                    websocketInfoGPMDP = new musicInfo();
                    websocketInfoGPMDP.Status = 0;
                };
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

        //For downloading the image, called in a thread in the onMessage for the websocket
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
                        if (coverOutputLocation == Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rainmeter/GPMDPPlugin/cover.png")
                        {
                            // Make sure the path folder exists if using it
                            System.IO.Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/Rainmeter/GPMDPPlugin");
                        }
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


        //These functions are related to elevating the GPMDP websocket to have remote status
        //Call sendGPMDPRemoteRequest to have GPMDP generate a 4 digit keycode, then getGPMDPAuthCode once you have recieved the code, and send authcode once GPMDP's websocket has sent you the perminate code
        //Should be mostly unused now see next group of functions for new automatic method
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

        //This is to prevent multiple writes from happening to the settings file
        private static bool fileIsAdjusted = false;
        //These get and adjust if needed the GPMDP settings
        private static void getGPMDPSettings()
        {
            try
            {
                string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google Play Music Desktop Player\\json_store\\.settings.json");

                using (StreamReader file = File.OpenText(settingsPath))
                using (JsonTextReader reader = new JsonTextReader(file))
                {
                    JObject settingsFile = (JObject)JToken.ReadFrom(reader);
                    reader.Close();
                    file.Close();

                    bool fileNeedsAdjusted = false;

                    bool fileContainsPlaybackAPISection = false;
                    bool fileContainsAuthcodeSection = false;

                    foreach (JToken setting in settingsFile.Children())
                    {
                        if (setting.Path.ToString().CompareTo("themeColor") == 0)
                        {
                            string settingsColor = setting.First.ToString();

                            try
                            {
                                if (settingsColor.ToLower().Contains("rgb"))
                                {
                                    lastKnownThemeColor = settingsColor.Substring(settingsColor.IndexOf("(") + 1, settingsColor.IndexOf(")") - settingsColor.IndexOf("(") - 1);
                                }
                                else
                                {
                                    //Cutoff the # that is at the beginning
                                    settingsColor = settingsColor.Substring(1);
                                    string r = Convert.ToInt32(settingsColor.Substring(0, 2), 16).ToString();
                                    string g = Convert.ToInt32(settingsColor.Substring(2, 2), 16).ToString();
                                    string b = Convert.ToInt32(settingsColor.Substring(4, 2), 16).ToString();

                                    lastKnownThemeColor = r + ", " + g + ", " + b;
                                }
                            }
                            catch (Exception e)
                            {
                                API.Log(API.LogType.Error, "Unable to convert the theme color from GPMDP settings file, report this issue on the GPMDP plugin github page");
                                API.Log(API.LogType.Debug, e.ToString());
                            }
                        }
                        else if (setting.Path.ToString().CompareTo("themeType") == 0)
                        {
                            if (setting.First.ToString().ToUpper().CompareTo("FULL") == 0)
                            {
                                lastKnownThemeType = 1;
                            }
                            else
                            {
                                lastKnownThemeType = 0;
                            }
                        }
                        else if (setting.Path.ToString().CompareTo("enableJSON_API") == 0 && !fileIsAdjusted)
                        {
                            if (setting.First.ToString().ToLower().Contains("false"))
                            {
                                setting.First.Replace("true");
                                fileNeedsAdjusted = true;
                            }
                        }
                        else if (setting.Path.ToString().CompareTo("playbackAPI") == 0 && !fileIsAdjusted)
                        {
                            fileContainsPlaybackAPISection = true;

                            if (setting.First.ToString().ToLower().Contains("false"))
                            {
                                setting.First.Replace("true");
                                fileNeedsAdjusted = true;
                            }
                        }
                        else if (setting.Path.ToString().CompareTo("authorized_devices") == 0 && !fileIsAdjusted)
                        {
                            fileContainsAuthcodeSection = true;
                            bool foundMatch = false;
                            foreach (JToken currAuthcode in setting.Children().Children())
                            {
                                if (currAuthcode.ToString().CompareTo(authcode) == 0)
                                {
                                    foundMatch = true;
                                }

                            }

                            if (!foundMatch)
                            {
                                //If no authcode found matching the one from the rainmeter settings kill GPMDP and make a new one

                                if (setting.First.Last != null)
                                {
                                    String newAuthcode = System.Guid.NewGuid().ToString();
                                    authcode = newAuthcode;

                                    setting.First.Last.AddAfterSelf(authcode);
                                    WritePrivateProfileString("GPMDPPlugin", "AuthCode", authcode, rainmeterFileSettingsLocation);

                                    fileNeedsAdjusted = true;
                                }
                                else
                                {
                                    fileContainsAuthcodeSection = false;
                                }
                            }
                        }
                    }

                    if (!fileContainsPlaybackAPISection && !fileIsAdjusted)
                    {
                        settingsFile.Add("playbackAPI", "true");

                        fileNeedsAdjusted = true;
                    }

                    if (!fileContainsAuthcodeSection && !fileIsAdjusted)
                    {
                        settingsFile.Remove("authorized_devices");

                        String newAuthcode = System.Guid.NewGuid().ToString();
                        authcode = newAuthcode;

                        JArray authcodes = new JArray(authcode);
                        settingsFile.Add("authorized_devices", authcodes);
                        WritePrivateProfileString("GPMDPPlugin", "AuthCode", authcode, rainmeterFileSettingsLocation);

                        fileNeedsAdjusted = true;
                    }

                    if (fileNeedsAdjusted && !fileIsAdjusted)
                    {
                        fileIsAdjusted = true;
                        adjustGPMDPSettings(settingsFile);
                    }
                    else
                    {
                        fileIsAdjusted = true;
                    }
                }
            }
            catch
            {
                API.Log(API.LogType.Error, "Unable to locate GPMDP settings file, you will need to use an authenication skin to get playback controls and custom theme colors will be unsupported");
            }
        }
        private static void adjustGPMDPSettings(JObject settingsFile)
        {
            try
            {
                foreach (var process in Process.GetProcessesByName("Google Play Music Desktop Player"))
                {
                    process.Kill();
                }

                string settingsPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Google Play Music Desktop Player\\json_store\\.settings.json");

                //Replace is to make it so it uses 4 spaces instead of 2 spaces like the normal file does
                File.WriteAllText(settingsPath, settingsFile.Root.ToString().Replace("  ", "    "));
            }
            catch
            {
                API.Log(API.LogType.Error, "Unable to write to GPMDP settings file, this will cause issues with automatic verification. Try closing GPMDP and refreshing your skin");
            }

            try
            {
                Process.Start(Environment.GetEnvironmentVariable("LocalAppData") + "\\GPMDP_3\\update.exe", "-processStart \"Google Play Music Desktop Player.exe\"");
            }
            catch
            {
                API.Log(API.LogType.Warning, "Unable to relaunch GPMDP after first run settings change, you will need to relaunch it manually");
            }
        }

        //These are functions that handle the sending of various GPMDP websocket commands
        //In theory if these were called before the websocket has been setup the could error but that would be impossible in rainmeter so adding the overhead for checks is unneeded.
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
        private static void GPMDPToggleRepeat()
        {
            String repeatString = "{\n";
            repeatString += "\"namespace\": \"playback\",\n";
            repeatString += "\"method\": \"toggleRepeat\"\n";
            repeatString += "}";
            ws.SendAsync(repeatString, null);
        }
        private static void GPMDPSetPosition(String percent)
        {
            int currentDuration = websocketInfoGPMDP.DurationInms;
            int timeInms = websocketInfoGPMDP.PositionInms;

            try
            {
                if (percent.Contains("-")) { timeInms = (int)Math.Round(timeInms - Convert.ToDouble(percent.Substring(percent.IndexOf("-") + 1)) * currentDuration / 100); ; }
                else if (percent.Contains("+")) { timeInms = (int)Math.Round(timeInms + Convert.ToDouble(percent.Substring(percent.IndexOf("+") + 1)) * currentDuration / 100); ; }
                else { timeInms = (int)Math.Round(Convert.ToDouble(percent) * currentDuration / 100); }
            }
            catch (Exception e)
            {
                API.Log(API.LogType.Error, "Unable to convert SetPosition bang value:" + percent + " to a valid position.");
                API.Log(API.LogType.Debug, e.ToString());
            }

            String positionString = "{\n";
            positionString += "\"namespace\": \"playback\",\n";
            positionString += "\"method\": \"setCurrentTime\",\n";
            positionString += "\"arguments\": [" + timeInms + "]\n";
            positionString += "}";
            ws.SendAsync(positionString, null);
        }
        private static void GPMDPToggleThumbsUp()
        {
            String ratingString = "{\n";
            ratingString += "\"namespace\": \"rating\",\n";
            ratingString += "\"method\": \"toggleThumbsUp\"\n";
            ratingString += "}";
            ws.SendAsync(ratingString, null);
        }
        private static void GPMDPToggleThumbsDown()
        {
            String ratingString = "{\n";
            ratingString += "\"namespace\": \"rating\",\n";
            ratingString += "\"method\": \"toggleThumbsDown\"\n";
            ratingString += "}";
            ws.SendAsync(ratingString, null);
        }
        private static void GPMDPSetRating(int rating)
        {
            if (rating == -1) { rating = 1; }
            else if (rating == 0) { rating = 3; }
            else if (rating == -1) { rating = 5; }

            String ratingString = "{\n";
            ratingString += "\"namespace\": \"rating\",\n";
            ratingString += "\"method\": \"setRating\",\n";
            ratingString += "\"arguments\": [" + rating + "]\n";
            ratingString += "}";
            ws.SendAsync(ratingString, null);
        }
        private static void GPMDPSetVolume(String volume)
        {
            int currentVolume = websocketInfoGPMDP.Volume;
            int volumeToSet = 100;

            try
            {
                if (volume.Contains("-")) { volumeToSet = currentVolume - Convert.ToInt16(volume.Substring(volume.IndexOf("-") + 1)); }
                else if (volume.Contains("+")) { volumeToSet = currentVolume + Convert.ToInt16(volume.Substring(volume.IndexOf("+") + 1)); }
                else { volumeToSet = Convert.ToInt16(volume); }
            }
            catch (Exception e)
            {
                API.Log(API.LogType.Error, "Unable to convert SetVolume bang value:" + volume + " to a valid volume.");
                API.Log(API.LogType.Debug, e.ToString());
            }

            if (volumeToSet > 100) { volumeToSet = 100; }
            else if (volumeToSet < 0) { volumeToSet = 0; }

            String volumeString = "{\n";
            volumeString += "\"namespace\": \"volume\",\n";
            volumeString += "\"method\": \"setVolume\",\n";
            volumeString += "\"arguments\": [" + volumeToSet + "]\n";
            volumeString += "}";
            ws.SendAsync(volumeString, null);
        }
        //UNUSED, turns out you can not do a manual request to get genre. Shelved until GPMDP supports it which they have no plans to. Also can not be used to get index or id
        private static void GPMDPGetExtraSongInfo()
        {
            //TODO change the requestID from being a constant to using an internal ID system.
            String playPauseString = "{\n";
            playPauseString += "\"namespace\": \"playback\",\n";
            playPauseString += "\"method\": \"getCurrentTrack\",\n";
            playPauseString += "\"requestID\": " + 1 + "\n";
            playPauseString += "}";
            ws.SendAsync(playPauseString, null);
        }
        private static void GPMDPSetTrack(int trackLoc)
        {
            if (websocketInfoGPMDP.Queue.Count > trackLoc)
            {
                //GPMDPGetQueueTracks();

                //TODO change the requestID from being a constant to using an internal ID system.
                //TODO Use objects for command strings instead of this trash, it was okay before when it was just a few hacky lines now its borderline unreadable Edit: Better now but still should probably be rewritten
                String queuePlaytrackString = "{\n";
                queuePlaytrackString += "\"namespace\": \"queue\",\n";
                queuePlaytrackString += "\"method\": \"playTrack\",\n";
                queuePlaytrackString += "\"arguments\": [\n" + websocketInfoGPMDP.Queue[trackLoc] + "\n],\n";
                queuePlaytrackString += "\"requestID\": " + 2 + "\n";
                queuePlaytrackString += "}";
                ws.SendAsync(queuePlaytrackString, null);
            }
        }

        //For updating the queue displayed to the user with new info
        private static void updateQueueInfo(JToken queueInfo)
        {
            websocketInfoGPMDP.Queue.Clear();
            //lastKnownQueueLocation = -1;

            //API.Log(API.LogType.Notice, "queue:" + currentValue);
            foreach (JToken track in queueInfo)
            {
                //TODO Check this code in edge cases since at least AlbumArt is not passed if it is the default cover
                if(track["title"].ToString() == websocketInfoGPMDP.Title && track["artist"].ToString() == websocketInfoGPMDP.Artist && track["album"].ToString() == websocketInfoGPMDP.Album)
                {
                    lastKnownQueueLocation = (int)(track["index"]) - 1;
                }

                websocketInfoGPMDP.Queue.Add(track);
            }

            //if(lastKnownQueueLocation == -1)
            //{
            //    API.Log(API.LogType.Error, "GPMDP was unable to locate current song in queue");
            //    lastKnownQueueLocation = 0;
            //}
        }
        //For updating where we are at in the queue when the queue has not changed
        //TODO Nag GPMDP devs to make it so I can identify current song in a less intensive way
        private static void updateQueueLoc()
        {
            if (queueUpdateThread.IsAlive)
            {
                queueUpdateThread.Join();
            }


            if (websocketInfoGPMDP.Queue.Count > 0)
            {
                bool atBeginning = false;
                bool atEnd = false;
                bool foundMatch = false;
                int currLoc = lastKnownQueueLocation;
                int increment = 1;

                if(currLoc >= websocketInfoGPMDP.Queue.Count)
                {
                    currLoc = websocketInfoGPMDP.Queue.Count - 1;
                }

                while (!(atBeginning && atEnd) && !foundMatch)
                {
                    if (!atEnd && !atBeginning)
                    {
                        JToken track = websocketInfoGPMDP.Queue[currLoc];

                        if (track["title"].ToString() == websocketInfoGPMDP.Title && track["artist"].ToString() == websocketInfoGPMDP.Artist && track["album"].ToString() == websocketInfoGPMDP.Album)
                        {
                            lastKnownQueueLocation = (int)(track["index"]) - 1;
                            foundMatch = true;
                        }

                        currLoc += increment;
                        increment *= -1;

                        if(increment > 0)
                        {
                            increment++;
                        }
                        else
                        {
                            increment--;
                        }

                        if (currLoc >= websocketInfoGPMDP.Queue.Count)
                        {
                            atEnd = true;
                            currLoc += increment;
                        }
                        if (currLoc < 0)
                        {
                            atBeginning = true;
                            currLoc += increment;
                        }
                    }
                    else if (!atEnd)
                    {
                        JToken track = websocketInfoGPMDP.Queue[currLoc];

                        if (track["title"].ToString() == websocketInfoGPMDP.Title && track["artist"].ToString() == websocketInfoGPMDP.Artist && track["album"].ToString() == websocketInfoGPMDP.Album)
                        {
                            lastKnownQueueLocation = (int)(track["index"]) - 1;
                            foundMatch = true;
                        }

                        currLoc++;
                        if(currLoc >= websocketInfoGPMDP.Queue.Count)
                        {
                            atEnd = true;
                        }
                    }
                    else if (!atBeginning)
                    {
                        JToken track = websocketInfoGPMDP.Queue[currLoc];

                        if (track["title"].ToString() == websocketInfoGPMDP.Title && track["artist"].ToString() == websocketInfoGPMDP.Artist && track["album"].ToString() == websocketInfoGPMDP.Album)
                        {
                            lastKnownQueueLocation = (int)(track["index"]) - 1;
                            foundMatch = true;
                        }

                        currLoc--;
                        if (currLoc < 0)
                        {
                            atBeginning = true;
                        }
                    }
                }

                if(!foundMatch)
                {
                    API.Log(API.LogType.Error, "GPMDP was unable to locate current song in queue");
                }
            }

        }

        //For interfacing with the Websocketinfo's queue, makes it so I dont have to keep two different queues.
        //TODO make this better
        private static string getSongInfoFromQueue(int queueLoc, QueueInfoType type)
        {
            string songInfo = "";

            if (queueLoc >= 0 && queueLoc < websocketInfoGPMDP.Queue.Count)
            {

                foreach (JProperty trackInfo in websocketInfoGPMDP.Queue[queueLoc])
                {
                    if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                    else if (trackInfo.Name.ToString().ToLower().CompareTo(type.ToString().ToLower()) == 0)
                    {
                        songInfo = trackInfo.First.ToString();
                    }
                }
            }
            else
            {
                if (type == QueueInfoType.Duration || type == QueueInfoType.PlayCount || type == QueueInfoType.Index)
                {
                    songInfo = "0";
                }
                else
                {
                    songInfo = "";
                }
            }
            return songInfo;
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
        internal Measure(Rainmeter.API api)
        {
            //If not setup get the rainmeter settings file location and load the authcode
            if (rainmeterFileSettingsLocation.Length == 0)
            {
                rainmeterFileSettingsLocation = api.GetSettingsFile();
                char[] authchar = new char[36];
                GetPrivateProfileString("GPMDPPlugin", "AuthCode", "", authchar, 37, rainmeterFileSettingsLocation);
                authcode = new String(authchar);
            }

            getGPMDPSettings();

            if (GPMInitThread.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                GPMInitThread.Start();
            }
        }

        internal void ExecuteBang(string args)
        {
            string bang = args.ToLowerInvariant();
            if (bang.Equals("playpause"))
            {
                GPMDPPlayPause();
            }
            else if (bang.Equals("next"))
            {
                GPMDPForward();
            }
            else if (bang.Equals("previous"))
            {
                GPMDPPrevious();
            }
            else if (bang.Equals("repeat"))
            {
                GPMDPToggleRepeat();
            }
            else if (bang.Equals("shuffle"))
            {
                GPMDPToggleShuffle();
            }
            else if (bang.Equals("togglethumbsup"))
            {
                GPMDPToggleThumbsUp();
            }
            else if (bang.Equals("togglethumbsdown"))
            {
                GPMDPToggleThumbsDown();
            }
            else if (bang.Contains("setrating"))
            {
                try
                {
                    int rating = Convert.ToInt16(args.Substring(args.LastIndexOf(" ")));
                    GPMDPSetRating(rating);
                }
                catch (Exception e)
                {
                    API.Log(API.LogType.Error, "Unable to convert the end of SetRating bang " + args.Substring(args.ToLower().LastIndexOf("setrating")) + " to a number");
                    API.Log(API.LogType.Debug, e.ToString());
                }
            }
            else if (bang.Contains("setposition"))
            {
                String percent = args.Substring(args.LastIndexOf(" "));
                GPMDPSetPosition(percent);
            }
            else if (bang.Contains("setvolume"))
            {
                String volume = args.Substring(args.LastIndexOf(" "));
                GPMDPSetVolume(volume);
            }
            else if (bang.Equals("openplayer"))
            {
                Process.Start(Environment.GetEnvironmentVariable("LocalAppData") + "\\GPMDP_3\\update.exe", "-processStart \"Google Play Music Desktop Player.exe\"");
            }
            else if (bang.Equals("closeplayer"))
            {
                foreach (var process in Process.GetProcessesByName("Google Play Music Desktop Player"))
                {
                    process.Kill();
                }
            }
            else if (bang.Equals("toggleplayer"))
            {
                Process[] GPMDPProcesses = Process.GetProcessesByName("Google Play Music Desktop Player");

                if (GPMDPProcesses.Length > 0)
                {
                    foreach (var process in GPMDPProcesses)
                    {
                        process.Kill();
                    }
                }
                else
                {
                    Process.Start(Environment.GetEnvironmentVariable("LocalAppData") + "\\GPMDP_3\\update.exe", "-processStart \"Google Play Music Desktop Player.exe\"");
                }
            }
            else if (bang.Contains("setsong"))
            {
                try
                {
                    int songLoc = Convert.ToInt32(args.Substring(args.LastIndexOf(" ")));

                    //If it is set to relative mode then add last known queue location
                    if (args.Substring(args.LastIndexOf(" ")).Contains("+") || args.Substring(args.LastIndexOf(" ")).Contains("-"))
                    {
                        songLoc += lastKnownQueueLocation;
                    }

                    //Sanity checks
                    if (songLoc < 0)
                    {
                        songLoc = 0;
                    }
                    else if (songLoc >= websocketInfoGPMDP.Queue.Count)
                    {
                        songLoc = websocketInfoGPMDP.Queue.Count - 1;
                    }

                    //TODO Fix songLoc to be relative and cap at extremes
                    GPMDPSetTrack(songLoc);
                }
                catch (Exception e)
                {
                    API.Log(API.LogType.Error, "Unable to convert setSong argument to integer from command:" + args);
                    API.Log(API.LogType.Debug, e.ToString());
                }
            }
            //else if (bang.Contains("setSongAbsolute"))
            //{
            //    String songLoc = args.Substring(args.LastIndexOf(" "));
            //}
            else if (bang.Contains("key"))
            {
                //Get the last 4 chars of the keycode, this should ensure that we always get it even when bang is a little off
                getGPMDPAuthCode(args.Substring(args.Length - 4, 4));
            }
            else
            {
                API.Log(API.LogType.Error, "GPMDPPlugin.dll: Invalid bang " + args);
            }
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

                //case "number":
                //    InfoType = MeasureInfoType.Number;
                //    break;
                //
                //case "year":
                //    InfoType = MeasureInfoType.Year;
                //    break;
                //
                //case "genre":
                //    InfoType = MeasureInfoType.Genre;
                //    break;

                case "cover":
                    InfoType = MeasureInfoType.Cover;
                    defaultCoverLocation = api.ReadPath("DefaultPath", "");
                    string temp = api.ReadPath("CoverPath", null);
                    if (temp.Length > 0)
                    {
                        coverOutputLocation = temp;
                    }
                    break;

                case "coverwebaddress":
                    InfoType = MeasureInfoType.CoverWebAddress;
                    break;

                case "duration":
                    InfoType = MeasureInfoType.Duration;
                    disableLeadingZero = api.ReadInt("DisableLeadingZero", 0);
                    includeMS = api.ReadInt("IncludeMS", 0);

                    break;

                case "position":
                    InfoType = MeasureInfoType.Position;
                    disableLeadingZero = api.ReadInt("DisableLeadingZero", 0);
                    includeMS = api.ReadInt("IncludeMS", 0);

                    break;

                case "progress":
                    InfoType = MeasureInfoType.Progress;
                    asDecimal = api.ReadInt("AsDecimal", 0);

                    if (asDecimal == 1)
                    {
                        maxValue = 1.0;
                    }
                    else
                    {
                        maxValue = 100.0;
                    }
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
                    maxValue = 100;
                    break;

                case "lyrics":
                    InfoType = MeasureInfoType.Lyrics;
                    break;

                case "themetype":
                    InfoType = MeasureInfoType.ThemeType;
                    break;

                case "themecolor":
                    InfoType = MeasureInfoType.ThemeColor;
                    break;

                case "queue":
                    InfoType = MeasureInfoType.Queue;

                    String queueLoc = api.ReadString("QueueLocation", "0");
                    disableLeadingZero = api.ReadInt("DisableLeadingZero", 0);

                    try
                    {
                        myQueueLocationToRead = Convert.ToInt16(queueLoc);
                    }
                    catch (Exception e)
                    {
                        API.Log(API.LogType.Error, "Unable to convert the queue location " + queueLoc + " to an integer, assuming current song");
                        API.Log(API.LogType.Debug, e.ToString());
                        myQueueLocationToRead = 0;
                    }

                    string queueType = api.ReadString("QueueType", "").ToLower();

                    foreach (QueueInfoType currType in Enum.GetValues(typeof(QueueInfoType)))
                    {
                        if (queueType.CompareTo(currType.ToString().ToLower()) == 0)
                        {
                            myQueueInfoType = currType;
                        }
                    }
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

        internal virtual double Update()
        {

            if (GPMReconnectThread.ThreadState == System.Threading.ThreadState.Stopped || GPMReconnectThread.ThreadState == System.Threading.ThreadState.Unstarted)
            {
                GPMReconnectThread = new Thread(Measure.isGPMDPWebsocketConnected);
                GPMReconnectThread.Start();
            }

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
                    if (asDecimal == 1)
                    {
                        return websocketInfoGPMDP.Progress / 100.0;
                    }
                    return websocketInfoGPMDP.Progress;
                case MeasureInfoType.Rating:
                    return websocketInfoGPMDP.Rating;
                case MeasureInfoType.ThemeType:
                    return lastKnownThemeType;
                case MeasureInfoType.Duration:
                    if(includeMS == 1)
                    {
                        return (double)websocketInfoGPMDP.DurationInms / 1000.0;
                    }
                    return Math.Floor(((double)websocketInfoGPMDP.DurationInms / 1000));
                case MeasureInfoType.Position:
                    if (includeMS == 1)
                    {
                        return (double)websocketInfoGPMDP.PositionInms / 1000.0;
                    }
                    return Math.Floor(((double)websocketInfoGPMDP.PositionInms / 1000));
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
                //case MeasureInfoType.Number:
                //    return websocketInfoGPMDP.Number;
                //case MeasureInfoType.Year:
                //    return websocketInfoGPMDP.Year;
                //case MeasureInfoType.Genre:
                //    return websocketInfoGPMDP.Genre;
                case MeasureInfoType.Cover:
                    if (websocketInfoGPMDP.Cover != null)
                    {
                        return websocketInfoGPMDP.Cover;
                    }
                    return defaultCoverLocation;
                case MeasureInfoType.CoverWebAddress:
                    return websocketInfoGPMDP.CoverWebAddress;
                case MeasureInfoType.Duration:
                    int trackSecondsDur = websocketInfoGPMDP.DurationInms / 1000;
                    int trackMinutesDur = trackSecondsDur / 60;
                    trackSecondsDur = trackSecondsDur % 60;

                    if (disableLeadingZero == 0)
                    {
                        return trackMinutesDur.ToString().PadLeft(2, '0') + ":" + trackSecondsDur.ToString().PadLeft(2, '0');
                    }
                    return trackMinutesDur.ToString().PadLeft(1, '0') + ":" + trackSecondsDur.ToString().PadLeft(2, '0');
                case MeasureInfoType.Position:
                    int trackSecondsPos = websocketInfoGPMDP.PositionInms / 1000;
                    int trackMinutesPos = trackSecondsPos / 60;
                    trackSecondsPos = trackSecondsPos % 60;

                    if (disableLeadingZero == 0)
                    {
                        return trackMinutesPos.ToString().PadLeft(2, '0') + ":" + trackSecondsPos.ToString().PadLeft(2, '0');
                    }
                    return trackMinutesPos.ToString().PadLeft(1, '0') + ":" + trackSecondsPos.ToString().PadLeft(2, '0');
                case MeasureInfoType.Lyrics:
                    return websocketInfoGPMDP.Lyrics;
                case MeasureInfoType.ThemeColor:
                    return lastKnownThemeColor;
                case MeasureInfoType.Queue:
                    int readLoc = lastKnownQueueLocation + myQueueLocationToRead;

                    if (myQueueInfoType == QueueInfoType.Duration)
                    {
                        try
                        {
                            int trackSecondsQueue = Convert.ToInt32(getSongInfoFromQueue(readLoc, myQueueInfoType)) / 1000;

                            int trackMinutesQueue = trackSecondsQueue / 60;
                            trackSecondsQueue = trackSecondsQueue % 60;

                            if (disableLeadingZero == 0)
                            {
                                return trackMinutesQueue.ToString().PadLeft(2, '0') + ":" + trackSecondsQueue.ToString().PadLeft(2, '0');
                            }
                            return trackMinutesQueue.ToString().PadLeft(1, '0') + ":" + trackSecondsQueue.ToString().PadLeft(2, '0');
                        }
                        catch (Exception e)
                        {
                            API.Log(API.LogType.Error, "Unable to convert the duration of a song in the queue from GPMDP, report this issue on the GPMDP plugin github page");
                            API.Log(API.LogType.Debug, e.ToString());
                        }
                    }
                    return getSongInfoFromQueue(readLoc, myQueueInfoType);

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
                case MeasureInfoType.ThemeType:
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
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure(new Rainmeter.API(rm))));
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
