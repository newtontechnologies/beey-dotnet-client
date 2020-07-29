BatchTranscriber
===

A console-app in .NET core to upload many files to Beey and get their transcription (trsx).

# Usage

`BatchTranscriber.exe {<directory with audio files>|<list of files>} <output directory> (threads=8) (logintoken=TOKEN) (settings=Settings.xml) (mode={batch|single}) (debug={no|yes}) (language=cs-CZ)`

* **First param.** is required, it is either a directory in which **all** the files will be sent to beey or a file in which case it is treated as a list of files. *(or as a single file, see 'Singlemode')*

* **Second param.** is required, it must be a directory in which the trsx files will be saved.

* Extra options can be set in whatever order, see 'Extra options'

## List of files
The so-called 'list of files' must be a normal text document where each line contains exactly one file (relative path to it)

## Example

To transcript everything in directory `audio` and save transcriptions in directory `trans`:

`BatchTranscriber.exe audio trans`

To transcript many files (list of them is `list.txt`) and save transcription in directory `trans`:

`BatchTranscriber.exe list.txt trans`

To transcript a single file `audio.mp3` and save transcription in directory `trans`:

`BatchTranscriber.exe audio.mp3 trans mode=single`

# Extra options

* **threads=X** maximum number of threads to be run at the same time. **(default: 8)**
* **logintoken=TOKEN** token to be used as an alternative to email/password
* **language=cs-CZ** this will override language in Settings.xml !
* **settings=Settings.xml** custom file to be used instead of Settings.xml **(default: Settings.xml)**
* **debug=yes** this will enable spamming the stdout with all the messages from all the threads. **(default: no)**
* **mode=single** only works if the first param. is a file, see 'Singlemode' **(default: batch)**

## Singlemode

Singlemode is activated when first param. is a file and `mode=single` is set.

In this case the first param. will be treated like a single audio file to be transcripted.

# Known issues

* **BatchTranscriber does not attempt to re-send files which failed.** If a thread fails (file not found/internet problem/...), a message will be printed to the console and the thread will terminate and will be assigned a new task
* **Files must not have the same name.** Or their trsx files will be overwritten.
* **There is no warning if you forget to switch on `mode=single`** If you put mp3 file as the first param and don't set `mode=single`, crazy things will happen as it will be treated like a list of files.