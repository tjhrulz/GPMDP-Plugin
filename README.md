#Google Play Music Desktop Player for Rainmeter
A plugin that makes available the music info [Google Play Music Desktop Player](https://www.googleplaymusicdesktopplayer.com/) to rainmeter skins.  
Has full media control support as well (Play/Pause, Next, Previous, Shuffle, Repeat, etc.)  
Included in this repo is a example skin that shows how to use every measure and bang.  
Current state: Standard media info feature set and controls.  
Future: Support for rating, next queued songs info, extra song info such as genre and release year, and lyrics planned for future versions.  
  
#Full list of supported measure and bang types  
###MEASURE TYPES
**Title, Artist, Album** - String of current playing songs info, blank if no info    
**Cover** - String of the path where the current album art is downloaded to, set attribute CoverPath to define where to download it, set attribute DefaultPath to set what image to show while waiting for the album art to download   
**CoverWebAddress** - String of URL location of current album art, useful for doing an onChangeAction as cover will update twice when the song changes this will only update once  
**Position, Duration** - String of how far into the song or how long the song is, formated MM:SS  
**Progress** - Integer of how far into the song you are as a percentage, note this and SetPosition only support integers if their is demand for decimal support I can make it happen  
**Repeat** - Integer of if GPMDP is set to repeat, 0 is no, 1 is repeat one song, 2 is repeat all  
**Shuffle** - Integer of if GPMDP is set to shuffle, 0 is no, 1 is yes **NOTE** GPMDP shuffle support is broken is some cases see issue [here](https://github.com/MarshallOfSound/Google-Play-Music-Desktop-Player-UNOFFICIAL-/issues/2193)  
**State** - Integer of the play state of GPMDP, 0 is stopped, 1 is playing, 2 is paused  
**ConnectionStatus** or **Status** - Integer status of the connection to GPMDP, -1 is plugin has not finished initializing and will become 0-2 in a moment, 0 is disconnected, 1 is connected but without remote control access, 2 is full connection  
I recommend including a skin to allow the user to enter in the 4 digit pin if you redistribute this plugin. In the future I will have a guide in the wiki walking through how to setup GPMDP, for the future just see the included installer and helper skin for how to inform your users. 

###BANGS
**SetPosition ###** - Where ### is an Integer between 0-100. Sets the what percent of the way through the song the song is   
**Previous, PlayPause, Next** - Self explanitory, note previous set the song back to the start before going back to the previous song, PlayPause is a toggle and there is not currently a play or pause that just does that if it is requested in the future I could add this but GPMDP does not support it
**Repeat** - Toggles through repeat modes, order goes None -> Repeat All -> Repeat One ->  
**Shuffle** - Toggles through shuffle modes, order goes None -> Shuffle All **NOTE** GPMDP shuffle support is broken is some cases see issue [here](https://github.com/MarshallOfSound/Google-Play-Music-Desktop-Player-UNOFFICIAL-/issues/2193)  
**key ####** or **keycode ####** - Where #### is the 4 digit authentication code for GPMDP used to elavate connection status from 1 to 2, note this only has to be done once per machine and then it is saved in the rainmeter.data file. If connection status is 0 then GPMDP is not setup and doing this will do nothing.  
  
  
Note: Please see included installer on how to grant the plugin access to media controls. If no info is shown please see helper for how to setup GPMDP, in the future the helper replaced with a link to the wiki with an explanation on how to set it up.
