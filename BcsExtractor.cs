using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace BcsExtractor
{
    class BcsExtractor
    {
        static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var extract = new Command("extract", "Extract .csv scripts from .bcs files. Extracts from every recognizable .bcs file if given a directory.")
            {
                new Argument<String>("input", "File or directory containing .bcs files."),
                new Option<String?>(new[] { "--output", "-o" }, "Output directory. Defaults to current working directory."),
                new Option(new[] { "--overwrite" }, "Overwrite existing files in the output path."),
                new Option(new[] { "--verbose", "-v" }, "Print more details."),
            };

            var format = new Command("format", "Format .csv scripts for word wrapping purposes. Works on every .csv file if given a directory.")
            {
                new Argument<String>("input", "File or directory containing .csv files."),
                new Option<String?>(new[] { "--output", "-o" }, "Output directory. Defaults to current working directory."),
                new Option(new[] { "--overwrite" }, "Overwrite existing files in the output path."),
                new Option(new[] { "--verbose", "-v" }, "Print more details."),
                new Option(new[] { "--keep-newlines" }, "Keep the old newlines(\\n) in the file, otherwise they are removed before formatting."),
                new Option<int>(new[] { "--wrap-length", "-wl" }, "Number of characters in a row until it should wrap. Defaults to 50."),
            };

            extract.Handler = CommandHandler.Create<String, String?, bool, bool>(Extract);
            format.Handler = CommandHandler.Create<String, String?, bool, bool, bool, int>(Format);

            var cmd = new RootCommand
            {
                extract,
                format
            };

            return cmd.InvokeAsync(args).Result;
        }

        internal static void Extract(String input, String? output, bool verbose, bool overwrite)
        {
            String outputDir;
            FileAttributes attr = File.GetAttributes(input);
            if (output != null)
                outputDir = Directory.CreateDirectory(output).FullName;
            else
                outputDir = Directory.GetCurrentDirectory();

            if (attr.HasFlag(FileAttributes.Directory))
            {
                DirectoryInfo dir = new DirectoryInfo(input);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                    ExtractFile(file.FullName, outputDir, verbose, overwrite);
            }
            else 
                ExtractFile(new FileInfo(input).FullName, outputDir, verbose, overwrite);
        }

        internal static void Format(String input, String? output, bool verbose, bool overwrite, bool keepNewlines, int wrapLength)
        {
            String outputDir;
            int wc = wrapLength;
            if (wc == 0)
                wc = 50;

            FileAttributes attr = File.GetAttributes(input);
            if (output != null) 
                outputDir = Directory.CreateDirectory(output).FullName;
            else
                outputDir = Directory.GetCurrentDirectory();


            if (attr.HasFlag(FileAttributes.Directory))
            {
                DirectoryInfo dir = new DirectoryInfo(input);
                FileInfo[] files = dir.GetFiles();
                foreach (FileInfo file in files)
                    FormatFile(file.FullName, outputDir, wc, verbose, overwrite, keepNewlines);
            }
            else
                FormatFile(new FileInfo(input).FullName, outputDir, wc, verbose, overwrite, keepNewlines);
        }

        internal static void FormatFile(String input, String output, int wrapCount, bool verbose, bool overwrite, bool keepNewlines)
        {
            if (Path.GetExtension(input) != ".csv") 
            {
                if (verbose == true)
                    Console.WriteLine("Skipping {0} because it is not a .csv file", input);
                return;
            }
            else if(overwrite == false && File.Exists(output + "/" + Path.GetFileName(input)))
            {
                Console.WriteLine("Skipping {0} because output file already exists", input);
                return;
            }

            String[] fields;
            String[] splitLine;
            String temp;
            int wordCount;

            using (TextFieldParser csvParser = new TextFieldParser(input, Encoding.GetEncoding(932)))
            {
                csvParser.CommentTokens = new string[] { "#" };
                csvParser.SetDelimiters(new string[] { "," });
                csvParser.HasFieldsEnclosedInQuotes = true;
                csvParser.TrimWhiteSpace = false;

                fields = csvParser.ReadFields();
                int textIndex = 0;

                foreach (String field in fields)
                {
                    if (field.Equals("%text%") || field.Equals("%text"))
                        break;
                    textIndex++;
                }

                if (textIndex >= fields.Length)
                {
                    if (verbose == true)
                        Console.WriteLine("Skipping {0} because it has no text field", input);
                    return;
                }

                using (StreamWriter sw = new StreamWriter(File.Open(output + "/" + Path.GetFileName(input), FileMode.Create), Encoding.GetEncoding(932))) 
                {
                    sw.Write(String.Join(",", fields));
                    sw.Write("\r\n");
                    while (!csvParser.EndOfData)
                    {
                        fields = csvParser.ReadFields();
                        if(keepNewlines == true)
                            splitLine = fields[textIndex].Split(" ");
                        else
                            splitLine = fields[textIndex].Replace("\\n", " ").Split(" ");
                        wordCount = 0;
                        temp = "";
                        foreach (String word in splitLine) 
                        {
                            if (temp.Length != 0) 
                            {
                                if (word.Length + 1 + wordCount > wrapCount) 
                                {
                                    temp += "\\n";
                                    wordCount = 0;
                                }
                                else
                                {
                                    temp += " ";
                                    wordCount++;
                                }
                            }
                            temp += word;
                            wordCount += word.Length;
                        }
                        fields[textIndex] = temp;
                        for (int i = 0; i < fields.Length; i++) 
                        {
                            if (fields[i].Contains(","))
                                fields[i] = "\"" + fields[i] + "\"";
                        }
                        sw.Write(String.Join(",", fields));
                        sw.Write("\r\n");
                    }
                }
            }
        }

        internal static void ExtractFile(String input, String output, bool verbose, bool overwrite)
        {
            String baseName = Path.GetFileNameWithoutExtension(input);
            String finalOutput = output + "/" + baseName + ".csv";
            String blowfishKey = "TLibDefKey";
            int bcsHeaderSize = 24;

            if (overwrite == false && File.Exists(finalOutput)) 
            {
                Console.WriteLine("Skipping {0} because it already exists", input);
                return;
            }

            byte[] resultBuffer = ReadBCSHeader(input);
            if ((char)resultBuffer[0] != 'T' ||
                (char)resultBuffer[1] != 'S' ||
                (char)resultBuffer[2] != 'V') 
            {
                if (verbose == true)
                    Console.WriteLine("Skipping {0} because it is not a .bcs file", input);
                return;
            }

            uint unpackedSize = BitConverter.ToUInt32(resultBuffer, 4);
            uint objectCount = BitConverter.ToUInt32(resultBuffer, 8);
            uint objectMark = BitConverter.ToUInt32(resultBuffer, 12);
            uint objectPartsCount = BitConverter.ToUInt32(resultBuffer, 16);
            uint bodySize = BitConverter.ToUInt32(resultBuffer, 20);

            using (FileStream fs = File.OpenRead(input))
            {
                uint firstTableSize = objectCount * 8; // Each object is 8 bytes
                uint secondTableSize = objectPartsCount * 8; // Index table
                uint tnkSize = unpackedSize - (firstTableSize + secondTableSize); // TNK portion is after the two tables
                byte[] unpacked = new byte[unpackedSize];
                byte[] indexTable = new byte[secondTableSize];
                byte[] tnkTable = new byte[tnkSize];

                using (var stream = new ProxyStream(fs))
                {
                    LzssUnpack(stream, bcsHeaderSize, unpacked);
                }

                // Only use of the first table is it tells how many columns there are
                int numCols = (int)BitConverter.ToUInt32(unpacked, 0);

                uint unpackedIndex = firstTableSize; // Start at index table
                for (int itIndex = 0; itIndex < secondTableSize; itIndex++)
                {
                    indexTable[itIndex] = unpacked[unpackedIndex];
                    unpackedIndex++;
                }

                for (int tnkIndex = 0; tnkIndex < tnkSize; tnkIndex++)
                {
                    tnkTable[tnkIndex] = unpacked[unpackedIndex];
                    unpackedIndex++;
                }

                // TNK has a 12 byte header
                byte[] tnkBody = new List<byte>(tnkTable).GetRange(12, tnkTable.Length - 12).ToArray();
                var blowfish = new Blowfish(Encoding.ASCII.GetBytes(blowfishKey));
                blowfish.Decipher(tnkBody, tnkBody.Length & ~7);

                int indexCurr = 0;
                List<byte> finalBytes = new List<byte>();
                String finalLine = "";
                String line;
                while (indexCurr <= indexTable.Length)
                {
                    line = BuildScriptLine(indexTable, tnkBody, indexCurr, numCols) + "\r\n";
                    finalLine += line;
                    indexCurr += (numCols * 8); // Each col is 8 bytes
                }

                // Write using shift-jis encoding
                using (StreamWriter sw = new StreamWriter(File.Open(finalOutput, FileMode.Create), Encoding.GetEncoding(932)))
                    sw.Write(finalLine);

                if (verbose == true)
                    Console.WriteLine("Finished extracting from {0}", input);
            }
        }

        internal static byte[] ReadBCSHeader(String path)
        {
            byte[] tempBuffer = new byte[0x100];
            using (FileStream fs = File.OpenRead(path))
                fs.Read(tempBuffer, 0, 24);

            return tempBuffer;
        }

        internal static String BuildScriptLine(byte[] script, byte[] tnkTable, int index, int numCols)
        {
            uint operand;
            uint value;
            String work;
            List<byte> part;
            int indexOffset = 0;
            String returnString = "";
            byte mask = 0b11;

            for (int curr = 0; curr < numCols; curr++)
            {
                operand = BitConverter.ToUInt32(script, index + indexOffset) & mask;
                indexOffset += 4;
                if (operand == 0x01) // Plain integer value
                {
                    value = BitConverter.ToUInt32(script, index + indexOffset);
                    returnString += value;
                }
                else if (operand == 0x03) // Offset into the TNK table of strings
                {
                    value = BitConverter.ToUInt32(script, index + indexOffset);
                    part = ReadUntil00(tnkTable, value);
                    work = Encoding.GetEncoding(932).GetString(part.ToArray());
                    if (work.Contains(","))
                        work = "\"" + work + "\"";
                    returnString += work;
                }
                else if (returnString.Length == 0 && operand == 0x00)
                    return returnString;

                if (curr < numCols - 1)
                    returnString += ",";
                indexOffset += 4;
            }

            return returnString;
        }

        internal static List<byte> ReadUntil00(byte[] toRead, uint start)
        {
            List<byte> ret = new List<byte>();
            int index = (int)start;
            byte curr = toRead[index];

            while (curr != 0x00)
            {
                // If there's a quote, add double quotes for csv escaping
                if (curr == 0x22)
                    ret.Add(curr);
                ret.Add(curr);
                index++;
                if (index >= toRead.Length)
                    break;
                curr = toRead[index];
            }

            return ret;
        }

        // This is basically LzssUnpack taken from GARBro, all credit to its creator morkt
        internal static void LzssUnpack(Stream input, int skip, byte[] output, bool invert = false)
        {
            const int frameMask = 0xFFF;
            byte[] frame = new byte[0x1000];
            int framePos = 0xFEE;
            int dst = 0;
            int ctl = 2; 
            input.Seek(skip, 0);
            while (dst < output.Length)
            {
                ctl >>= 1;
                if (1 == ctl)
                {
                    ctl = input.ReadByte();
                    if (-1 == ctl)
                        break;
                    ctl |= 0x100;
                }

                // New character
                if (0 != (ctl & 1))
                {
                    int b = input.ReadByte();
                    if (-1 == b)
                        break;
                    frame[framePos++ & frameMask] = (byte)b;
                    if (invert)
                        output[dst++] = (byte)(~(b));
                    else
                        output[dst++] = (byte)b;
                }
                // Already seen
                else
                {
                    int lo = input.ReadByte();
                    if (-1 == lo)
                        break;
                    int hi = input.ReadByte();
                    if (-1 == hi)
                        break;
                    int offset = (hi & 0xf0) << 4 | lo;
                    int count = Math.Min((~hi & 0xF) + 3, output.Length - dst);
                    while (count-- > 0)
                    {
                        byte v = frame[offset++ & frameMask];
                        frame[framePos++ & frameMask] = v;
                        if (invert)
                            output[dst++] = (byte)(~(v));
                        else
                            output[dst++] = v;
                    }
                }
            }
        }
    }
}
