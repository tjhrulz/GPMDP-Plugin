using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
//using System.Net.WebSockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using WebSocketSharp;
using Rainmeter;
using System.Text;
using System.IO;
using System.Net;
using System.Threading;

namespace BetterMusicPlugin
{
    internal class Measure
    {
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
            Volume
        }

        enum MeasurePlayerType
        {
            GPMDP,
            Soundnode,
            ChromeMusicInfoXposed,
            Dynamic
        }

        //To add a new player add the name here and add the function to handle its info and a call for its info in the Update functions loop
        //private static MeasurePlayerType[] allSupportedPlayers = { MeasurePlayerType.GPMDP, MeasurePlayerType.Soundnode, MeasurePlayerType.ChromeMusicInfoXposed };
        musicInfo[] latestInfo;
        int latestInfoSource;

        class musicInfo
        {
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
            public string Volume { get; set; }
        }

        //Initialized to title so that if a bad type is given they will get that and an error
        private MeasureInfoType InfoType = MeasureInfoType.Title;
        private MeasurePlayerType PlayerType = MeasurePlayerType.Dynamic;
        public static WebSocket ws;
        private const String supportedAPIVersion = "1.1.0";
        private static musicInfo websocketInfoGPMDP = new musicInfo();
        private static string defaultCoverLocation;
        private static string coverOutputLocation;
        private static Thread GPMInitThread = new Thread(Measure.GPMDPWebsocketCreator);
        private static bool websocketState = false;
        private static bool remoteState = false;

        enum GPMInfoSupported
        {
            api_version,
            track,
            time,
            playstate,
            connect
        }

        public static void GPMDPWebsocketCreator()
        {
            if (ws == null || ws.IsAlive)
            {
                //List<Object> requestAccess = new List<Object>();
                //Object accessObject = new { Namespace = "connect", Method = "connect", Arguments = "GPMDP plugin for Rainmeter"};
                //requestAccess.Add(accessObject);

                // API.Log(API.LogType.Warning, requestAccess.ToString());

                //var test = JsonConvert.SerializeObject(requestAccess, Formatting.Indented);

                ws = new WebSocket("ws://localhost:5672");
                bool acceptedVersion = false;

                ws.OnMessage += (sender, d) =>
                {
                    String type = d.Data.Substring(12, d.Data.IndexOf(",") - 13);
                    GPMInfoSupported typeEnum;
                    Console.WriteLine(type);

                    if (Enum.TryParse(type.ToLower(), out typeEnum))
                    {
                        JObject data = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(d.Data);
                        JArray arrayData = new JArray(data);

                        foreach (JToken token in arrayData)
                        {
                            //Console.WriteLine(token.First.Last);
                            //API.Log(API.LogType.Notice, token.First.Last.ToString());

                            JToken currentProperty = token.First.Last;
                            JToken currentValue = token.Last.Last;

                            if (currentProperty.ToString().ToLower().CompareTo(GPMInfoSupported.api_version.ToString()) == 0)
                            {
                                String versionNumber = currentValue.ToString();

                                if (versionNumber.Substring(0, versionNumber.IndexOf(".")).CompareTo(supportedAPIVersion.Substring(0, versionNumber.IndexOf("."))) == 0)
                                {
                                    //Console.WriteLine("Version match");
                                    acceptedVersion = true;
                                }
                                else
                                {
                                    //TODO Have a rainmeter attribute flag to supress this error and attempt to continue working
                                    //API.Log(API.LogType.Error, "GPMDP Websocket API version is: " + versionNumber + " this plugin was built for version " + supportedAPIVersion);
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
                                    else if (coverOutputLocation != null && trackInfo.Name.ToString().ToLower().CompareTo("albumart") == 0)
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

                                Console.WriteLine("cInfo:" + connectionInfo);
                                Console.WriteLine(currentProperty.ToString());
                            }
                        }
                    }
                };


                ws.OnClose += (sender, d) => websocketState = false;
                ws.OnOpen += (sender, d) => websocketState = true;

                ws.ConnectAsync();
                //Console.ReadKey(true);
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

        //Note before calling this you should set websocketInfoGPMDP.Cover to the default cover location to help mitagate OnChange not being called for measures that have a low update rate, also launch this on a different thread
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

        internal Measure()
        {
            latestInfo = new musicInfo[Enum.GetNames(typeof(MeasurePlayerType)).Length];
            latestInfoSource = -1;
            //ws = new WebSocket("ws://localhost:5672");

            //var sb = new StringBuilder();
            //var sw = new StringWriter(sb);
            //var JSONWriter = new JsonTextWriter(sw);
            //
            //JSONWriter.WritePropertyName("Namespace");
            //JSONWriter.WriteValue("connect");
            //JSONWriter.WritePropertyName("Method");
            //JSONWriter.WriteValue("connect");
            //JSONWriter.WritePropertyName("Arguments");
            //JSONWriter.WriteValue("GPMDP API Tester");
            //
            //openConnectionString = JSONWriter.ToString();

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
            string infoType = api.ReadString("PlayerInfo", "title");
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

                case "status":
                    InfoType = MeasureInfoType.Status;
                    break;

                case "volume":
                    InfoType = MeasureInfoType.Volume;
                    break;

                default:
                    API.Log(API.LogType.Error, "BetterMusicPlugin.dll: InfoType=" + infoType + " not valid");
                    break;
            }

            string playerType = api.ReadString("PlayerType", "dynamic");
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

                case "chromemusicinfoxposed":
                    PlayerType = MeasurePlayerType.ChromeMusicInfoXposed;
                    break;

                default:
                    API.Log(API.LogType.Error, "BetterMusicPlugin.dll: PlayerType=" + playerType + " not valid");
                    break;
            }
        }

        ////Functions specific to AIMP player
        //private static Boolean isAIMPRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getAIMPInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test AIMP Song", Artist = "Test AIMP Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to CAD player
        //private static Boolean isCADRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getCADInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test CAD Song", Artist = "Test CAD Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to iTunes player
        //private static Boolean isiTunesRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getiTunesInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test iTunes Song", Artist = "Test iTunes Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to MediaMonkey player
        //private static Boolean isMediaMonkeyRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getMediaMonkeyInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test MediaMonkey Song", Artist = "Test MediaMonkey Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to Winamp player
        //private static Boolean isWinampRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getWinampInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test Winamp Song", Artist = "Test Winamp Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to WMP player
        //private static Boolean isWMPRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getWMPInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test WMP Song", Artist = "Test WMP Artist" };
        //
        //    return currInfo;
        //}
        //
        ////Functions specific to Spotify player
        //private static Boolean isSpotifyRunning()
        //{
        //    return true;
        //}
        //private static musicInfo getSpotifyInfo()
        //{
        //    musicInfo currInfo = new musicInfo { Title = "Test Spotify Song", Artist = "Test Spotify Artist" };
        //
        //    return currInfo;
        //}

        //Functions specific to GPMDP player
        private static Boolean isGPMDPRunning()
        {
            if (websocketState == false && ws != null)
            {
                ws.ConnectAsync();
            }
            //if (ws != null && ws.ReadyState == WebSocketState.Open && remoteState == false)
            //{
            //
            //    String ConnectionString = "{\n";
            //    ConnectionString += "\"namespace\": \"connect\",\n";
            //    ConnectionString += "\"method\": \"connect\",\n";
            //    ConnectionString += "\"arguments\": \"[\"GPMDP API Tester\"]\"\n";
            //    ConnectionString += "}";
            //
            //    //ConnectionString += "\"arguments\": \"[\"GPMDP API Tester\", \"3867\"]\"\n";
            //
            //    Console.WriteLine(ConnectionString);
            //    ws.SendAsync(ConnectionString, connection => GPMDPPlayPause());
            //    remoteState = true;
            //}

            return websocketState;
        }
        private static musicInfo getGPMDPInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test GPMDP Song", Artist = "Test GPMDP Artist" };

            currInfo = websocketInfoGPMDP;

            return currInfo;
        }
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

        //Functions specific to Soundnode player
        private static Boolean isSoundnodeRunning()
        {
            //System.Diagnostics.Process[] SoundnodeStatus = System.Diagnostics.Process.GetProcessesByName("Soundnode");

            //return (SoundnodeStatus.Length > 0) ? true : false;
            return false;
        }
        private static musicInfo getSoundnodeInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test Soundnode Song", Artist = "Test Soundnode Artist" };

            return currInfo;
        }

        //Functions specific to ChromeMusicInfoXposed player
        private static Boolean isChromeMusicInfoXposedRunning()
        {
            return false;
        }
        private static musicInfo getChromeMusicInfoXposedInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test ChromeMusicInfoXposed Song", Artist = "Test ChromeMusicInfoXposed Artist" };

            return currInfo;
        }

        internal virtual double Update()
        {
            //Dummy pseudocode to give an idea how the update will work
            if (PlayerType == MeasurePlayerType.Dynamic)
            {
                int newSongInfoSource = -1;
                for (int i = 0; i < Enum.GetNames(typeof(MeasurePlayerType)).Length -1; i++)
                {
                    //if (i == (int) MeasurePlayerType.AIMP)
                    //{
                    //    if (isAIMPRunning())
                    //    {
                    //        musicInfo newInfo = getAIMPInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.CAD)
                    //{
                    //    if (isCADRunning())
                    //    {
                    //        musicInfo newInfo = getCADInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.iTunes)
                    //{
                    //    if (isiTunesRunning())
                    //    {
                    //        musicInfo newInfo = getiTunesInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.MediaMonkey)
                    //{
                    //    if (isMediaMonkeyRunning())
                    //    {
                    //        musicInfo newInfo = getMediaMonkeyInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.Winamp)
                    //{
                    //    if (isWinampRunning())
                    //    {
                    //        musicInfo newInfo = getWinampInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.WMP)
                    //{
                    //    if (isWMPRunning())
                    //    {
                    //        musicInfo newInfo = getWMPInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else if (i == (int) MeasurePlayerType.Spotify)
                    //{
                    //    if (isSpotifyRunning())
                    //    {
                    //        musicInfo newInfo = getSpotifyInfo();
                    //        if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                    //        {
                    //            latestInfo[i] = newInfo;
                    //            newSongInfoSource = i;
                    //        }
                    //    }
                    //}
                    //else 
                    if (i == (int)MeasurePlayerType.GPMDP)
                    {
                        if (isGPMDPRunning())
                        {
                            musicInfo newInfo = getGPMDPInfo();
                            if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                            {
                                latestInfo[i] = newInfo;
                                newSongInfoSource = i;
                            }
                        }
                    }
                    else if (i == (int)MeasurePlayerType.Soundnode)
                    {
                        if (isSoundnodeRunning())
                        {
                            musicInfo newInfo = getSoundnodeInfo();
                            if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                            {
                                latestInfo[i] = newInfo;
                                newSongInfoSource = i;
                            }
                        }
                    }
                    else if (i == (int)MeasurePlayerType.ChromeMusicInfoXposed)
                    {
                        if (isChromeMusicInfoXposedRunning())
                        {
                            musicInfo newInfo = getChromeMusicInfoXposedInfo();
                            if (newInfo.Title != null && (latestInfo[i] == null || !newInfo.Title.Equals(latestInfo[i].Title)))
                            {
                                latestInfo[i] = newInfo;
                                newSongInfoSource = i;
                            }
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Media player defined but not handled");
                    }
                }
                if (newSongInfoSource >= 0)
                {
                    latestInfoSource = newSongInfoSource;
                }
            }
            else
            {
                if (PlayerType == MeasurePlayerType.GPMDP)
                {
                    if (isGPMDPRunning())
                    {
                        musicInfo newInfo = getGPMDPInfo();
                        if (newInfo.Title != null && (latestInfo[(int)MeasurePlayerType.GPMDP] == null || !newInfo.Title.Equals(latestInfo[(int)MeasurePlayerType.GPMDP].Title)))
                        {
                            latestInfo[(int)MeasurePlayerType.GPMDP] = newInfo;
                            latestInfoSource = (int)MeasurePlayerType.GPMDP;
                        }
                    }
                }
                else if (PlayerType == MeasurePlayerType.Soundnode)
                {
                    if (isSoundnodeRunning())
                    {
                        musicInfo newInfo = getSoundnodeInfo();
                        if (newInfo.Title != null && (latestInfo[(int)MeasurePlayerType.Soundnode] == null || !newInfo.Title.Equals(latestInfo[(int)MeasurePlayerType.Soundnode].Title)))
                        {
                            latestInfo[(int)MeasurePlayerType.Soundnode] = newInfo;
                            latestInfoSource = (int)MeasurePlayerType.Soundnode;
                        }
                    }
                }
                else if (PlayerType == MeasurePlayerType.ChromeMusicInfoXposed)
                {
                    if (isChromeMusicInfoXposedRunning())
                    {
                        musicInfo newInfo = getChromeMusicInfoXposedInfo();
                        if (newInfo.Title != null && (latestInfo[(int)MeasurePlayerType.ChromeMusicInfoXposed] == null || !newInfo.Title.Equals(latestInfo[(int)MeasurePlayerType.ChromeMusicInfoXposed].Title)))
                        {
                            latestInfo[(int)MeasurePlayerType.ChromeMusicInfoXposed] = newInfo;
                            latestInfoSource = (int)MeasurePlayerType.ChromeMusicInfoXposed;
                        }
                    }
                }
                else
                {
                    System.Console.WriteLine("Media player defined but not handled");
                }
            }

            switch (InfoType)
            {
                case MeasureInfoType.State:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].State;
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
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Artist;
                    }
                    return "";
            
                case MeasureInfoType.Album:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Album;
                    }
                    return "";
            
                case MeasureInfoType.Title:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Title;
                    }
                    return "";
            
                case MeasureInfoType.Number:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Number;
                    }
                    return "";
            
                case MeasureInfoType.Year:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Year;
                    }
                    return "";
            
                case MeasureInfoType.Genre:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Genre;
                    }
                    return "";

                case MeasureInfoType.Cover:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Cover;
                    }
                    return "";

                case MeasureInfoType.CoverWebAddress:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].CoverWebAddress;
                    }
                    return "";

                case MeasureInfoType.Duration:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Duration;
                    }
                    return "";

                case MeasureInfoType.Position:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Position;
                    }
                    return "";

                case MeasureInfoType.Progress:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Progress;
                    }
                    return "";
            
                case MeasureInfoType.Rating:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Rating;
                    }
                    return "";
            
                case MeasureInfoType.Repeat:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Repeat;
                    }
                    return "";
            
                case MeasureInfoType.Shuffle:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Shuffle;
                    }
                    return "";
            
                case MeasureInfoType.Status:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Status;
                    }
                    return "";
            
                case MeasureInfoType.Volume:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Volume;
                    }
                    return "";
            
                default:
                    API.Log(API.LogType.Error, "BetterMusicPlugin.dll: Type not valid");
                    break;
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
    }
}
