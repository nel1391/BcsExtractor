# Differences with Rune visual novels

In Rune visual novels, the first and second sections are identical. However the third one is not encrypted with blowfish. Instead it is compressed with lzss, the same lzss that the entire `.bcs` file is initially compressed with. This means that the string section in the `.bcs` of Rune visual novels are compressed twice.

`bcsextractor.exe` should be able to extract the scripts from the Rune games, but it looks like other things also change so the folder replacement trick that can be used in Tanuki Soft and Kaeru Soft games is not guaranteed to work.

I've only tried on Musume Shimai, and it appeared to try opening the folder as a '.g2' archive and failed. The only other instance I know of is a previous translation of Hatsukoi, where they were able to use the folder replacement but the game was older and used '.g' archives.
There might be some way to get it to work, but I did not look too hard. If anyone was interested, I would start there and try to see if the folder replacement can work.

Otherwise, you'd have to turn the csvs back to bcs, and then put the bcs files back in a '.g2' archive.
