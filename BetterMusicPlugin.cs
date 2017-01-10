using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rainmeter;

namespace BetterMusicPlugin
{
    public static class Plugin
    {
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
        private static String[] getAIMPInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to CAD player
        private static Boolean isCADRunning()
        {
            return true;
        }
        private static String[] getCADInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to iTunes player
        private static Boolean isiTunesRunning()
        {
            return true;
        }
        private static String[] getiTunesInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to MediaMonkey player
        private static Boolean isMediaMonkeyRunning()
        {
            return true;
        }
        private static String[] getMediaMonkeyInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to Winamp player
        private static Boolean isWinampRunning()
        {
            return true;
        }
        private static String[] getWinampInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to WMP player
        private static Boolean isWMPRunning()
        {
            return true;
        }
        private static String[] getWMPInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to Spotify player
        private static Boolean isSpotifyRunning()
        {
            return true;
        }
        private static String[] getSpotifyInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to GPMDP player
        private static Boolean isGPMDPRunning()
        {
            return true;
        }
        private static String[] getGPMDPInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to Soundnode player
        private static Boolean isSoundnodeRunning()
        {
            return true;
        }
        private static String[] getSoundnodeInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        //Functions specific to ChromeMusicInfoXposed player
        private static Boolean isChromeMusicInfoXposedRunning()
        {
            return true;
        }
        private static String[] getChromeMusicInfoXposedInfo()
        {
            String[] currInfo = { "Test AIMP Song", "Test AIMP Artist" };

            return currInfo;
        }

        [DllExport]
        public static double Update(IntPtr data)
        {
            Measure measure = (Measure)GCHandle.FromIntPtr(data).Target;

            //Dummy pseudocode to give an idea how the update will work

            for(int i = 0; i < allSupportedPlayers.Length; i++)
            {
                if (allSupportedPlayers[i] == "AIMP")
                {
                    if(isAIMPRunning())
                    {
                        String[] newInfo = getAIMPInfo();
                        if(newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {
                            
                        }
                    }
                }
                else if (allSupportedPlayers[i] == "CAD")
                {
                    if (isCADRunning())
                    {
                        String[] newInfo = getCADInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "iTunes")
                {
                    if (isiTunesRunning())
                    {
                        String[] newInfo = getiTunesInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "MediaMonkey")
                {
                    if (isMediaMonkeyRunning())
                    {
                        String[] newInfo = getMediaMonkeyInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Winamp")
                {
                    if (isWinampRunning())
                    {
                        String[] newInfo = getWinampInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "WMP")
                {
                    if (isWMPRunning())
                    {
                        String[] newInfo = getWMPInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Spotify")
                {
                    if (isSpotifyRunning())
                    {
                        String[] newInfo = getSpotifyInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "GPMDP")
                {
                    if (isGPMDPRunning())
                    {
                        String[] newInfo = getGPMDPInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else if (allSupportedPlayers[i] == "Soundnode")
                {
                    if (isSoundnodeRunning())
                    {
                        String[] newInfo = getSoundnodeInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }

                }
                else if (allSupportedPlayers[i] == "ChromeMusicInfoXposed")
                {
                    if (isChromeMusicInfoXposedRunning())
                    {
                        String[] newInfo = getChromeMusicInfoXposedInfo();
                        if (newInfo[0].Equals(latestInfo[i].Name) || latestInfo[i].Name == null)
                        {

                        }
                    }
                }
                else
                {
                    System.Console.WriteLine("Media player defined but not handled");
                }
            }

            return measure.Update();
        }
    }

    class musicInfo
    {
        public string Name, Artist, Album, AlbumArt;
        
    }
}
