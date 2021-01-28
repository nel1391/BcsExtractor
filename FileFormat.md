# Documentation of `.bcs` format

The file itself is lzss compressed, and after decompression it is separated into 3 distinct sections. The header is as follows:

```
struct BCSHeader
{
char magic[4]; // TSV
unsigned int uncompressedSize;
unsigned int firstSectionObjectCount;
unsigned int objectMark;
unsigned int secondSectionObjectCount;
unsigned int thirdSectionSize;
};
```

The first section is not that useful, it consists of consecutive entries that are split in two parts, each 4 bytes. The first part will be the number of columns in the eventual csv, the second will be an offset. The offset just increases by the number of columns in each consecutive entry.
The second section is an index that is used to build the csv. Like the first section, it consists of consecutive entries split into two parts, each 4 bytes. The first part is an operand, denoting what should be done.

- `0x01` means that the value is a plain 32-bit integer, the second part of the entry is then that integer.
- `0x03` means that it is an offset into the third section, the table of strings.
- `0x00` means this field is empty, and should be left blank.

Each entry corresponds to one field in one row of the csv. Thus, to build one row of the csv, an equal amount of entries as the number of columns must be read.

The third section is the table of strings. However, it actually appears to be a separate file format entirely. This is also encrypted with blowfish using the key `TLibDefKey`. After decryption, the all the strings in the csv come from offsets into this section.

# Differences with Rune visual novels

In Rune visual novels, the first and second sections are identical. However the third one is not encrypted with blowfish. Instead it is compressed with lzss, the same lzss that the entire `.bcs` file is initially compressed with. This means that the string section in the `.bcs` of Rune visual novels are compressed twice.

I don't believe there are too many other differences and I assume if you try the same folder replacement trick, it will work. Rune uses a different archive than `.tac` but I also don't believe that should matter. 

Regardless, if anyone seriously wants to translate one of the Rune visual novels, leave a request here and I can add support for their `.bcs` files.
