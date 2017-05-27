# Google Play Music Desktop Player for Rainmeter Plugin
A rainmeter plugin that adds support for the music info and playback controls from [Google Play Music Desktop Player](https://www.googleplaymusicdesktopplayer.com/).
Included in this repo is a example skin that shows how to use every measure and bang as well as a more fleshed out skin to show the possibilities of this plugin.

### Current state:

- Standard media information (title, artist, album, cover, position, duration, progress)
- Standard media controls (play, pause, forward/rewind, shuffle, rewind, volume)
- Full Queue & lyrics support
- Rating support (thumbs up/thumbs down)
- Theming support (fetch colors from GPMDP themes)
- Automatic authentification to GPMDP

### Future additions:
- Performance improvements when not using queue, add support for accessing and setting songs in queue in a non relative fashion 

## Measure types:

- `Title, Artist, Album`

  String of current playing songs info, blank if no info.    

- `Cover`

  String that points to current album art, while downloading or if no album art points to the path of the default.
  **Note:** The image resolution can be up to 512 x 512 and also the image has not to be a square.
  
  **Attributes:**  
  CoverPath - A system path to where to store the album art.  
  DefaultPath - A system path to what image to use when downloading the album art.
  
- `CoverWebAddress`

  String of URL location of current album art, useful for doing an onChangeAction as cover will update twice when the song changes. This will only update once.
  
- `Position, Duration`

  String of how far into the song or how long the song is, formated MM:SS. Position has a MaxValue of duration
  
  **Attributes:**  
  DisableLeadingZero - If set to 1 strings will be formatted as M:SS 
  
- `Progress`

  Double of how far into the song you are as a percentage. To clarify that number is formated ##.##### and has a predefined max of 100.00.
  
  **Attributes:**  
  AsDecimal - If set to 1 changes scale of progress from 0-100 to 0-1, also changes MaxValue to 1 

- `Repeat`

  Integer of if GPMDP is set to repeat. 0 is no, 1 is repeat one song, 2 is repeat all.
  
- `Shuffle`

  Integer of if GPMDP is set to shuffle. 0 is no, 1 is yes.
  
- `Rating`

  Integer of the rating of the song. -1 is thumbs down, 0 is no rating, 1 is thumbs up.
  
- `Volume`
  
  Integer between 0-100 of what the current volume is set in GPMDP.
  
- `Lyrics`

  String of the lyrics of the song.
  **Note:** While downloading the lyrics or if there are none known this string will be empty. Thus this changes twice on any song with lyrics and only once on a song without lyrics.
  
- `Queue`

  String of an info type from a location in the relative queue.
  
  **Attributes:**  
  QueueLocation - An integer that is the song relative to the current song you want. Now queue length is dynamic so similar to before if the queue was not long enough if you try to access a location not in the queue you will get either "" "00:00" or "0" depending on the info type. So QueueLocation=-1 will give you info from the last song.  
  QueueType - A string that is either Title, Artist, Album, AlbumArt, Duration, PlayCount, or Index. **Note** duration accepts same flags as normal
  
- `themetype`

  Integer of the type of theme GPMDP is using. 0 is white, 1 is black.
  
- `themecolor`

  String of RGB value without transparency of the theme color in GPMDP.
  
- `State`

  Integer of the play state of GPMDP. 0 is stopped, 1 is playing, 2 is paused.
  
- `ConnectionStatus` or `Status`

  Integer status of the connection to GPMDP.
  
  -1 is plugin has not finished initializing and will become 0-2 in a moment, 0 is disconnected, 1 is connected but without remote control access, 2 is full connection.
  
  Authentication should now be fully automatic so an authentication skin should not be necessary, but in the event it stays 1 the automatic authentication has failed.

## Bangs:

- `SetPosition ##.####`

  Where ##.#### is a Double between 0-100. Sets the what percent of the way through the song the song is. Add + or - in front to set the position relatively.
  
- `SetVolume ###`

  Where ### is a Integer beween 0-100, add + or - in front to set the volume relatively.
  
- `Previous`, `PlayPause`, `Next`

  Self explanitory.
  **Note:** Previous sets the song back to the start before going back to the previous song, PlayPause is a toggle and there is not currently a play or pause that just does that if it is requested in the future I could hack together a way to add this but GPMDP does not support it.
  
- `Repeat`

  Toggles through repeat modes, order goes None -> Repeat All -> Repeat One -> 
  
- `Shuffle`

  Toggles through shuffle modes, order goes None -> Shuffle All
  
- `ToggleThumbsUp` and `ToggleThumbsDown`

  Toggles the song being thumbed up or down, to set it to a specific state see SetRating.
  
- `SetRating #`

  when # is an integer, -1 is thumbs down, 0 is no rating, 1 is thumbs up.
    
- `SetSong #`

  when # is an integer, sets what song to play in the current queue. So if # was 5 it will play the song 5 songs from now or if # is -2 it will play the song 2 songs ago. **Note** if you try to set a song outside the range of the queue it will clamp to the first or last song in the queue.
  
- `OpenPlayer`, `ClosePlayer` and `TogglePlayer`
  
  Opens, closes or toggles GPMDP. Useful since to correctly launch GPMDP requires pointing to the update.exe and adding an argument or changing it on every update.

#### Deprecated
- `key ####` or `keycode ####` - **Deprecated, authentication is now automatic unless settings file can not be found**

  Where #### is the 4 digit authentication code for GPMDP used to elavate connection status from 1 to 2, **Note:** this only has to be done once per machine and then it is saved in the rainmeter.data file. If connection status is 0 then GPMDP is not setup and doing this will do nothing. 

## List of current bugs and oddities:
Similar to the spotify plugin all measures share the same cover download location so do not use the absolute path to your cover location, however unlike spotify your default cover location will always be your one. See issue [here](https://github.com/tjhrulz/GPMDP-Plugin/issues/1) for more info.
