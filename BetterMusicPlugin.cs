using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;

namespace BetterMusicPlugin
{
    internal class Measure
    {
        internal virtual void Dispose()
        {

        }

        internal virtual void Reload(Rainmeter.API api, ref double maxValue)
        {

        }

        internal virtual double Update()
        {
            return 0.0;
        }
    }

    public static class Plugin
    {
        //To add a new player add the name here and add the function to handle its info and a call for its info in the Update functions loop
        private static String[] allSupportedPlayers = { "AIMP", "CAD", "iTunes", "MediaMonkey", "Winamp", "WMP", "Spotify", "GPMDP", "Soundnode", "ChromeMusicInfoXposed" };
        private static musicInfo[] latestInfo;

        [DllExport]
        public static void Initialize(ref IntPtr data, IntPtr rm)
        {
            data = GCHandle.ToIntPtr(GCHandle.Alloc(new Measure()));

            latestInfo = new musicInfo[allSupportedPlayers.Length];
        }

        [DllExport]
        public static void Finalize(IntPtr data)
        {
            GCHandle.FromIntPtr(data).Free();
        }

        [DllExport]
        public static void Reload(IntPtr data, IntPtr rm, ref double maxValue)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;
            measure.Reload(new Rainmeter.API(rm), ref maxValue);
        }

        //Functions specific to AIMP player
        private static Boolean isAIMPRunning()
        {
            return true;
        }
        private static musicInfo getAIMPInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test AIMP Song", Artist = "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to CAD player
        private static Boolean isCADRunning()
        {
            return true;
        }
        private static musicInfo getCADInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test CAD Song", Artist = "Test CAD Artist" };

            return currInfo;
        }

        //Functions specific to iTunes player
        private static Boolean isiTunesRunning()
        {
            return true;
        }
        private static musicInfo getiTunesInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test iTunes Song", Artist = "Test iTunes Artist" };

            return currInfo;
        }

        //Functions specific to MediaMonkey player
        private static Boolean isMediaMonkeyRunning()
        {
            return true;
        }
        private static musicInfo getMediaMonkeyInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test MediaMonkey Song", Artist = "Test MediaMonkey Artist" };

            return currInfo;
        }

        //Functions specific to Winamp player
        private static Boolean isWinampRunning()
        {
            return true;
        }
        private static musicInfo getWinampInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test Winamp Song", Artist = "Test Winamp Artist" };

            return currInfo;
        }

        //Functions specific to WMP player
        private static Boolean isWMPRunning()
        {
            return true;
        }
        private static musicInfo getWMPInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test WMP Song", Artist = "Test WMP Artist" };

            return currInfo;
        }

        //Functions specific to Spotify player
        private static Boolean isSpotifyRunning()
        {
            return true;
        }
        private static musicInfo getSpotifyInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test Spotify Song", Artist = "Test Spotify Artist" };

            return currInfo;
        }

        //Functions specific to GPMDP player
        private static Boolean isGPMDPRunning()
        {
            return true;
        }
        private static musicInfo getGPMDPInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test GPMDP Song", Artist = "Test GPMDP Artist" };

            return currInfo;
        }

        //Functions specific to Soundnode player
        private static Boolean isSoundnodeRunning()
        {
            return true;
        }
        private static musicInfo getSoundnodeInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test Soundnode Song", Artist = "Test Soundnode Artist" };

            return currInfo;
        }

        //Functions specific to ChromeMusicInfoXposed player
        private static Boolean isChromeMusicInfoXposedRunning()
        {
            return true;
        }
        private static musicInfo getChromeMusicInfoXposedInfo()
        {
            musicInfo currInfo = new musicInfo { Name = "Test ChromeMusicInfoXposed Song", Artist = "Test ChromeMusicInfoXposed Artist" };

            return currInfo;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            //Dummy pseudocode to give an idea how the update will work

            int newSongInfoSource = -1;

            for(int i = 0; i < allSupportedPlayers.Length; i++)
            {
                if (allSupportedPlayers[i] == "AIMP")
                {
                    if(isAIMPRunning())
                    {
                        musicInfo newInfo = getAIMPInfo();
                        if(!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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
                        if (!newInfo.Name.Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
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

                if(newSongInfoSource >= 0)
                {
                    i = allSupportedPlayers.Length;
                }

            }

            

            return measure.Update();
        }
    }

    class musicInfo
    {
        public string Name { get; set; }
        public string Artist { get; set; }
        public string Album { get; set; }
        public string AlbumArt { get; set; }
    }
}
