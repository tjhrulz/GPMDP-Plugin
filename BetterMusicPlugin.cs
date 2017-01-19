using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;

namespace BetterMusicPlugin
{
    internal class Measure
    {
        //To add a new player add the name here and add the function to handle its info and a call for its info in the Update functions loop
        private static String[] allSupportedPlayers = {"GPMDP", "Soundnode", "ChromeMusicInfoXposed" };
        private static musicInfo[] latestInfo;
        int latestInfoSource = -1;

        enum MeasureType
        {
            Artist,
            Album,
            Title,
            Number,
            Year,
            Genre,
            Cover,
            File,
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

        class musicInfo
        {
            public string Artist { get; set; }
            public string Album { get; set; }
            public string Title { get; set; }
            public string Number { get; set; }
            public string Year { get; set; }
            public string Genre { get; set; }
            public string Cover { get; set; }
            public string File { get; set; }
            public string Duration { get; set; }
            public string Position { get; set; }
            public string Progress { get; set; }
            public string Rating { get; set; }
            public string Repeat { get; set; }
            public string Shuffle { get; set; }
            public string State { get; set; }
            public string Status { get; set; }
            public string Volume { get; set; }
        }

        private MeasureType Type = MeasureType.Artist;

        internal Measure()
        {
            latestInfo = new musicInfo[allSupportedPlayers.Length];
        }

        internal virtual void Dispose()
        {

        }

        internal virtual void Reload(Rainmeter.API api, ref double maxValue)
        {
            string type = api.ReadString("Type", "");
            switch (type.ToLowerInvariant())
            {
                case "artist":
                    Type = MeasureType.Artist;
                    break;

                case "album":
                    Type = MeasureType.Album;
                    break;

                case "title":
                    Type = MeasureType.Title;
                    break;

                case "number":
                    Type = MeasureType.Number;
                    break;

                case "year":
                    Type = MeasureType.Year;
                    break;

                case "genre":
                    Type = MeasureType.Genre;
                    break;

                case "cover":
                    Type = MeasureType.Cover;
                    break;

                case "file":
                    Type = MeasureType.File;
                    break;

                case "duration":
                    Type = MeasureType.Duration;
                    break;

                case "position":
                    Type = MeasureType.Position;
                    break;

                case "progress":
                    Type = MeasureType.Progress;
                    break;

                case "rating":
                    Type = MeasureType.Rating;
                    break;

                case "repeat":
                    Type = MeasureType.Repeat;
                    break;

                case "shuffle":
                    Type = MeasureType.Shuffle;
                    break;

                case "state":
                    Type = MeasureType.State;
                    break;

                case "status":
                    Type = MeasureType.Status;
                    break;

                case "volume":
                    Type = MeasureType.Volume;
                    break;

                default:
                    API.Log(API.LogType.Error, "BetterMusicPlugin.dll: Type=" + type + " not valid");
                    break;
            }

        }

        //Functions specific to AIMP player
        private static Boolean isAIMPRunning()
        {
            return true;
        }
        private static musicInfo getAIMPInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test AIMP Song", Artist = "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to CAD player
        private static Boolean isCADRunning()
        {
            return true;
        }
        private static musicInfo getCADInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test CAD Song", Artist = "Test CAD Artist" };

            return currInfo;
        }

        //Functions specific to iTunes player
        private static Boolean isiTunesRunning()
        {
            return true;
        }
        private static musicInfo getiTunesInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test iTunes Song", Artist = "Test iTunes Artist" };

            return currInfo;
        }

        //Functions specific to MediaMonkey player
        private static Boolean isMediaMonkeyRunning()
        {
            return true;
        }
        private static musicInfo getMediaMonkeyInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test MediaMonkey Song", Artist = "Test MediaMonkey Artist" };

            return currInfo;
        }

        //Functions specific to Winamp player
        private static Boolean isWinampRunning()
        {
            return true;
        }
        private static musicInfo getWinampInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test Winamp Song", Artist = "Test Winamp Artist" };

            return currInfo;
        }

        //Functions specific to WMP player
        private static Boolean isWMPRunning()
        {
            return true;
        }
        private static musicInfo getWMPInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test WMP Song", Artist = "Test WMP Artist" };

            return currInfo;
        }

        //Functions specific to Spotify player
        private static Boolean isSpotifyRunning()
        {
            return true;
        }
        private static musicInfo getSpotifyInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test Spotify Song", Artist = "Test Spotify Artist" };

            return currInfo;
        }

        //Functions specific to GPMDP player
        private static Boolean isGPMDPRunning()
        {
            return true;
        }
        private static musicInfo getGPMDPInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test GPMDP Song", Artist = "Test GPMDP Artist" };

            return currInfo;
        }

        //Functions specific to Soundnode player
        private static Boolean isSoundnodeRunning()
        {
            return true;
        }
        private static musicInfo getSoundnodeInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test Soundnode Song", Artist = "Test Soundnode Artist" };

            return currInfo;
        }

        //Functions specific to ChromeMusicInfoXposed player
        private static Boolean isChromeMusicInfoXposedRunning()
        {
            return true;
        }
        private static musicInfo getChromeMusicInfoXposedInfo()
        {
            musicInfo currInfo = new musicInfo { Title = "Test ChromeMusicInfoXposed Song", Artist = "Test ChromeMusicInfoXposed Artist" };

            return currInfo;
        }

        internal virtual double Update()
        {
            //Dummy pseudocode to give an idea how the update will work

            int newSongInfoSource = -1;

            for (int i = 0; i < allSupportedPlayers.Length; i++)
            {
                if (allSupportedPlayers[i] == "AIMP")
                {
                    if (isAIMPRunning())
                    {
                        musicInfo newInfo = getAIMPInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "CAD")
                {
                    if (isCADRunning())
                    {
                        musicInfo newInfo = getCADInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "iTunes")
                {
                    if (isiTunesRunning())
                    {
                        musicInfo newInfo = getiTunesInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "MediaMonkey")
                {
                    if (isMediaMonkeyRunning())
                    {
                        musicInfo newInfo = getMediaMonkeyInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Winamp")
                {
                    if (isWinampRunning())
                    {
                        musicInfo newInfo = getWinampInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "WMP")
                {
                    if (isWMPRunning())
                    {
                        musicInfo newInfo = getWMPInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Spotify")
                {
                    if (isSpotifyRunning())
                    {
                        musicInfo newInfo = getSpotifyInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "GPMDP")
                {
                    if (isGPMDPRunning())
                    {
                        musicInfo newInfo = getGPMDPInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Soundnode")
                {
                    if (isSoundnodeRunning())
                    {
                        musicInfo newInfo = getSoundnodeInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
                        {
                            latestInfo[i] = newInfo;
                            newSongInfoSource = i;
                        }
                    }

                }
                else if (allSupportedPlayers[i] == "ChromeMusicInfoXposed")
                {
                    if (isChromeMusicInfoXposedRunning())
                    {
                        musicInfo newInfo = getChromeMusicInfoXposedInfo();
                        if (!newInfo.Title.Equals(latestInfo[i].Title) || latestInfo[i].Title == null)
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

                if (newSongInfoSource >= 0)
                {
                    i = allSupportedPlayers.Length;
                    latestInfoSource = newSongInfoSource;
                }

            }

            return 0.5;
        }

        internal string GetString()
        {

            switch (Type)
            {
                case MeasureType.Artist:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Artist;
                    }
                    return "";

                case MeasureType.Album:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Album;
                    }
                    return "";

                case MeasureType.Title:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Title;
                    }
                    return "";

                case MeasureType.Number:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Number;
                    }
                    return "";

                case MeasureType.Year:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Year;
                    }
                    return "";

                case MeasureType.Genre:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Genre;
                    }
                    return "";

                case MeasureType.Cover:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Cover;
                    }
                    return "";

                case MeasureType.File:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].File;
                    }
                    return "";

                case MeasureType.Duration:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Duration;
                    }
                    return "";

                case MeasureType.Position:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Position;
                    }
                    return "";

                case MeasureType.Progress:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Progress;
                    }
                    return "";

                case MeasureType.Rating:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Rating;
                    }
                    return "";

                case MeasureType.Repeat:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Repeat;
                    }
                    return "";

                case MeasureType.Shuffle:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Shuffle;
                    }
                    return "";

                case MeasureType.State:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].State;
                    }
                    return "";

                case MeasureType.Status:
                    if (latestInfoSource >= 0)
                    {
                        return latestInfo[latestInfoSource].Status;
                    }
                    return "";

                case MeasureType.Volume:
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
