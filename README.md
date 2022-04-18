# BcsExtractor

Extracts scripts from Tanuki Soft and Kaeru Soft visual novels, specifically `.bcs` files.

Script extraction should work for every Tanuki Soft, Kaeru Soft, and Rune visual novel. The folder replacement
described here to put the extracted csv scripts back in the games should work for all Tanuki Soft and Kaeru Soft
visual novels, but it is not guaranteed to work for all Rune visual novels.

# Replacing the original scripts

1. Extract the `.bcs` files from the archives, generally they are in `datascn.tac`, `scenario.g2`, or `scenario.g`. I recommend using GARBro for this, but other tools will work and might be necessary depending on how old the game is.
2. Run `bcsextractor.exe` using extract and point it to the `.bcs` files.

```
bcsextractor.exe extract bcs_files -o put_csvs_here
```

3. Delete or move the original scenario archive(ie. `datascn.tac`) in the visual novel's main directory.
4. Make a new folder with the same name as the scenario archive(ie. `datascn.tac`) as replacement.
5. Place the csvs generated by `bcsextractor.exe` into the new folder created.

# Translating text

To translate the text, all you need to do is replace the japanese text in the csvs with other text. The game should automatically pick them up and work without issue. Mostly you will replace text under the `%text%` fields, but names and choices can also be translated. 

Note that since this is a csv, you MUST wrap text that contains a comma in quotation marks like so:
```
10900 ,,,,,,,,,,,,,,,,,,,,"Text that has, a comma"
```

Be careful not to change a command or variable, the game might break if it can't find those. 

`bcsextractor.exe` also has a format command that adds word wrapping for ease of use. See `bcsextractor.exe format -h`.

# Troubleshooting

- Q: GARBro extracts the `.bcs` files as garbled strings instead of something like `04haruna.bcs`.
- A: Due to how the `.tac` extraction works, unless GARBro knows what the name is going to be before it extracts it, it can't determine the name. It keeps a file in `GameData/tanuki.lst` in its installation directory that is a list of all the known Tanuki Soft/Kaeru Soft `.bcs` filenames. If you extract `_project.csv`, it should tell you all the `.bcs` filenames for the scripts in that game. Add them to GARBro's `GameData/tanuki.lst` and GARBro should recognize it after. I've added an expanded `tanuki.lst` in this repo as well.

- Q: All the emote sprites disappeared.
- A: `_emote.csv` is currently broken, just place the original `_emote.bcs` in the `datascn.tac` folder instead. There is nothing to translate in `_emote.bcs` anyway. The extractor should refuse to extract emote.bcs due to this.

- Q: The game doesn't work after replacing the scripts.
- A: Make sure the folder has the exact same name as the scenario archive like `datascn.tac` or `scenario.g2`. If the game is a Rune visual novel, see the linked documentation as it's not guaranteed to work.

# Documentation

[.bcs format](FileFormat.md)

[Differences with Rune visual novels](Rune.md)
