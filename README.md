# Google Play Music Desktop Player for Rainmeter
A plugin that makes available the music info [Google Play Music Desktop Player](https://www.googleplaymusicdesktopplayer.com/) to rainmeter skins.  
Has full media control support as well (Play/Pause, Next, Previous, Shuffle, Repeat, etc.)  
Included in this repo is a example skin that shows how to use every measure and bang.  
Current state: Standard media info feature set and controls. Rating and Lyrics support added, Queue support just added  
Future: Dynamically sized queue so you can go more than 10 into the past or future as well as performance improvements when no skin is using the queue

**Note:** Please see included installer on how to grant the plugin access to media controls. If no info is shown please see helper for how to setup GPMDP, in the future the helper will be replaced with a link to the wiki with an explanation on how to set it up.

## Full list of supported measure and bang types  
### MEASURE TYPES
**Title, Artist, Album** - String of current playing songs info, blank if no info    
**Cover** - String that points to current album art, while downloading or if no album art points to the path of the default. Atributes:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;CoverPath - a system path to where to store the album  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;DefaultPath - a system path to what image to use when downloading the album art   
**CoverWebAddress** - String of URL location of current album art, useful for doing an onChangeAction as cover will update twice when the song changes this will only update once  
**Position, Duration** - String of how far into the song or how long the song is, formated MM:SS  
**Progress** - Double of how far into the song you are as a percentage. To clarify that number is formated ##.##### and has a predefined max of 100.00.  
**Repeat** - Integer of if GPMDP is set to repeat, 0 is no, 1 is repeat one song, 2 is repeat all  
**Shuffle** - Integer of if GPMDP is set to shuffle, 0 is no, 1 is yes  
**Rating** - Integer of the rating of the song, -1 is thumbs down, 0 is no rating, 1 is thumbs up  
**Volume** - Integer between 0-100 of what the current volume is oin GPMDP
**Lyrics** - String of the lyrics of the song. **Note** While downloading the lyrics or if there are none know this string will be "" Thus this changes twice on any song with lyrics and only once on a song without lyrics  
**Queue** - String of an info type from a location in the relative queue Attributes:  
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;QueueLocation - an integer between -10 and 10 that is the song relative to the current song you want   
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;QueueType - A string that is either Title, Artist, Album, AlbumArt, Duration, PlayCount, or Index and is what info you want back from the queue   
**themetype** - Integer of the type of theme GPMDP is using, 0 is white, 1 is black  
**themecolor** - String of RGB value without transparency of the theme color in GPMDP **Note** custom colors are not supported  
**State** - Integer of the play state of GPMDP, 0 is stopped, 1 is playing, 2 is paused  
**ConnectionStatus** or **Status** - Integer status of the connection to GPMDP, -1 is plugin has not finished initializing and will become 0-2 in a moment, 0 is disconnected, 1 is connected but without remote control access, 2 is full connection  
I recommend including a skin to allow the user to enter in the 4 digit pin if you redistribute this plugin. In the future I will have a guide in the wiki walking through how to setup GPMDP, for the future just see the included installer and helper skin for how to inform your users.

### BANGS
**SetPosition ##.####** - Where ##.#### is a Double between 0-100. Sets the what percent of the way through the song the song is.  
**SetVolume ###** - Where ### is a Integer beween 0-100, add + or - in front to set the volume relatively
**Note** this and Progress just started supporting double's instead of int, if you need an int or are having issues contact me.   
**Previous, PlayPause, Next** - Self explanitory, note previous set the song back to the start before going back to the previous song, PlayPause is a toggle and there is not currently a play or pause that just does that if it is requested in the future I could add this but GPMDP does not support it  
**Repeat** - Toggles through repeat modes, order goes None -> Repeat All -> Repeat One ->  
**Shuffle** - Toggles through shuffle modes, order goes None -> Shuffle All **NOTE** GPMDP shuffle support is broken is some cases see issue [here](https://github.com/MarshallOfSound/Google-Play-Music-Desktop-Player-UNOFFICIAL-/issues/2193)  
**ToggleThumbsUp** and **ToggleThumbsDown** - Toggles the song being thumbed up or down, to set it to a specific state see SetRating  
**SetRating #** when # is an integer, -1 is thumbs down, 0 is no rating, 1 is thumbs up.  
**key ####** or **keycode ####** - Where #### is the 4 digit authentication code for GPMDP used to elavate connection status from 1 to 2, note this only has to be done once per machine and then it is saved in the rainmeter.data file. If connection status is 0 then GPMDP is not setup and doing this will do nothing.  

## List of current bugs:
If the theme color in GPMDP is a custom color the theme color is never sent. This is an issue with GPMDP's websocket API, seeing as these are undocumented and were added at the request of someone making an android remote companion app and I have bothered the GPMDP guys with enough issue request I have not opened a issue yet.  
All measures share the same cover and default cover location, (The spotify plugin is the same way) so if you are using two different GPMDP skins they will share those images. This is going to be a lot of work to fix and may have an impact on performance so this is low priority. So dont hard code images to use that path, use the MeasureCover instead as it correctly reflects them path.
