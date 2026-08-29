# MSM6650 Database Decoder
This tool is able to decode the audio database EPROM binaries used for the OKI MSM6650 

However, it was more specifically created to decode the announcements on a British Rail Class 365 announcement unit.

Examples of the decoded announcements can be found in the main files under "Announcements".

The Class 365 announcement EPROM binaries can be found in the main files under "Binaries".

## Phrase Definition Table
The database binaries start with a phrase table at hex address 0x800 which can hold up to 128 phrases. 

Each definition contains 4 bytes, a 0x07 byte then a big-endian 24 bit address for the starting byte of that phrase's audio data in the database. 

If a phrase index is blank and no phrase exists for it, the four bytes will all be 0x00.

## Audio Encoding Format
The audio is encoded with Dialogic ADPCM. However, block bytes are inserted into the audio.

The first byte for each phrase will start on a block byte, so the address pointed to by the phrase table will always be a block byte. This byte will be the remaining number of audio bytes left in the phrase, limited to 255 (0xFF).

Due to this, most block bytes in each phrase will be 0xFF, apart from the last two. As the remaining audio bytes in the phrase becomes below 255 (0xFF), the block byte will change to show the amount of remaining audio bytes.
There is an additional block byte which is 0x00 to mark the end of the phrase.

This is presumably done so then the MSM6650 knows exactly how many audio bytes to process, In fact during some experimentation of custom audio, I failed to put these bytes in and the MSM6650 continued to play audio into the next phrase as all the phrase audio data is placed one after each other.

I wrote a decoder/encoder for the Dialogic ADPCM format (although only the decoder is used here, I have used the encoder for other projects so it does work!) it's nearly 100% copied from the Dialogic ADPCM Algorithm document pseudocode found in this document: https://web.archive.org/web/20260314195903/https://www.mp3-tech.org/programmer/docs/adpcm.pdf (Since writing the code and creating this git repo the original website has gone down so an internet archive link will have to suffice!)
