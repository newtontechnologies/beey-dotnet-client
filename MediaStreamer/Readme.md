MediaStreamer Library
====

# Usage

To get audio stream from a local file and skip first 40 seconds (MP3):

`Stream stream = new LocalSource<MP3Format>("file.mp3", 40).GetStream();`

To get audio stream via direct url, skip first minute and only next 40 minutes (MP3):

`Stream stream = new WebSource<MP3Format>("https://server.com/video.mp3",TimeSpan.FromMinutes(1),TimeSpan.FromMinutes(40)).GetStream();`


# Library structure
* `MediaSource` is the main abstract class; its children perform source-specific tasks (downloading & parsing stream, opening file, ...)

* `MediaFormat` is abstract class; its children perform format-specific tasks (skipping X seconds, trimming audio, ...)


Examples of `MediaSource` children (derivated classes) are `LocalSource`,`WebSource` and `YouTubeSource`.

An example of `MediaFormat` child is `MP3Format`.


* When initializing  `MediaSource`-derived classes a type of `MediaFormat` children is passed.
*(`MediaSource` will use it to perform format-specific tasks)*
