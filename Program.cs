using Microsoft.VisualBasic;
using System.Text;
using NAudio.Wave;
using NAudio.Lame;

class Program
{
    static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Error: No arguments provided.");
            Console.WriteLine("MSM6650Decoder.exe - A tool to decode MSM6650 ADPCM binaries into PCM audio.");
            Console.WriteLine("Usage: MSM6650Decoder.exe <args>");
            Console.WriteLine("Args:");
            Console.WriteLine("Input Binaries    : -i \"1.bin,2.bin,3.bin,4.bin etc\" : Required (Input binaries must be in same directory and should be entered in a comma seperated list with no spaces.)");
            Console.WriteLine("Input Sample Rate : -s \"8/16 etc\"                    : Required (Number in Khz.)");
            Console.WriteLine("Input Sample Rate : -f \"WAV/MP3 etc\"                 : Optional (WAV or MP3, defaults to WAV.)");
            Console.WriteLine("Output Directory  : -o \"Output Directory\"            : Optional (Defaults to current directory.)");
            return;
        }

        List<string> inputBinaries = new List<string>();
        int sampleRate = 0;
        string outputFormat = "WAV";
        string outputDirectory = Directory.GetCurrentDirectory();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-i":
                    if (i + 1 < args.Length)
                    {
                        inputBinaries.AddRange(args[++i].Split(','));
                    }
                    break;

                case "-s":
                    if (i + 1 < args.Length)
                    {
                        sampleRate = int.Parse((float.Parse(args[++i]) * 1000).ToString());
                    }
                    break;

                case "-f":
                    if (i + 1 < args.Length)
                    {
                        outputFormat = args[++i].ToUpper();
                    }
                    break;

                case "-o":
                    if (i + 1 < args.Length)
                    {
                        outputDirectory = args[++i];
                        if (!Directory.Exists(outputDirectory))
                        {
                            Directory.CreateDirectory(outputDirectory);
                        }
                    }
                    break;

                case "-h":
                case "--help":
                    Console.WriteLine("MSM6650Decoder.exe - A tool to decode MSM6650 ADPCM binaries into PCM audio.");
                    Console.WriteLine("Usage: MSM6650Decoder.exe <args>");
                    Console.WriteLine("Args:");
                    Console.WriteLine("Input Binaries    : -i \"1.bin,2.bin,3.bin,4.bin etc\" : Required (Input binaries must be in same directory and should be entered in a comma seperated list with no spaces.)");
                    Console.WriteLine("Input Sample Rate : -s \"8/16 etc\"                    : Required (Number in Khz.)");
                    Console.WriteLine("Input Sample Rate : -f \"WAV/MP3 etc\"                 : Optional (WAV or MP3, defaults to WAV.)");
                    Console.WriteLine("Output Directory  : -o \"Output Directory\"            : Optional (Defaults to current directory.)");
                    return;
            }
        }

        if(inputBinaries.Count == 0)
        {
            Console.WriteLine("Error: Input binaries not provided. Use -i to specify the input binaries in a comma seperated list with no spaces.");
            return;
        }
        if (sampleRate == 0)
        {
            Console.WriteLine("Error: Input sample rate not provided. Use -s to specify the input sample rate in Khz.");
            return;
        }

        byte[] combinedBinary = combineBinaries(inputBinaries);

        DecodeSegments(combinedBinary, sampleRate, outputFormat, outputDirectory);
    }

    private static void DecodeSegments(byte[] combinedBinary, int sampleRate, string outputFormat, string outputDirectory)
    {
        List<List<int>> segments = new List<List<int>>();
        List<int> segmentPosValues = new List<int>();
        int segmentPos = 0;
        int pos = 0x800;
        while (true)
        {
            if (combinedBinary[pos] == 0)
            {
                pos += 4;
            }
            else
            {
                if(combinedBinary[pos] == 0x07)
                {
                    int value = (combinedBinary[pos + 1] << 16) | (combinedBinary[pos + 2] << 8) | combinedBinary[pos + 3];
                    List<int> decodedSamples = DecodeSeg(value, combinedBinary);
                    segments.Add(decodedSamples);
                    segmentPosValues.Add(segmentPos);
                    pos += 4;
                }
            }
            segmentPos++;
            if(pos >= 0xA00)
            {
                break;
            }
        }

        for (int i = 0; i < segments.Count; i++)
        {
            List<int> segment = segments[i];
            int segmentPosValue = segmentPosValues[i];
            string outputFileName = Path.Combine(outputDirectory, $"Segment_{segmentPosValue}.{outputFormat.ToLower()}");
            if (outputFormat == "WAV")
            {
                ExportWav(segment, sampleRate, outputFileName);
            }
            else if (outputFormat == "MP3")
            {
                ExportMp3(segment, sampleRate, outputFileName);
            }
            else
            {
                Console.WriteLine($"Error: Unsupported output format '{outputFormat}'. Supported formats are WAV and MP3.");
                return;
            }
        }
        Console.WriteLine($"Decoding complete. {segments.Count} segments decoded and saved to '{outputDirectory}'.");
    }

    static void ExportWav(List<int> samples, int sampleRate, string filename)
    {
        var format = new WaveFormat(sampleRate, 16, 1);

        using (var writer = new WaveFileWriter(filename, format))
        {
            foreach (int sample in samples)
            {
                writer.WriteByte((byte)(sample & 0xFF));
                writer.WriteByte((byte)((sample >> 8) & 0xFF));
            }
        }
    }

    static void ExportMp3(List<int> samples, int sampleRate, string filename)
    {
        var format = new WaveFormat(sampleRate, 16, 1);

        using var writer = new LameMP3FileWriter(
            filename,
            format,
            LAMEPreset.STANDARD);

        foreach (int sample in samples)
        {
            short pcm = (short)Math.Clamp(sample, short.MinValue, short.MaxValue);

            writer.WriteByte((byte)(pcm & 0xFF));
            writer.WriteByte((byte)((pcm >> 8) & 0xFF));
        }
    }

    private static List<int> DecodeSeg(int startingPos, byte[] combinedBinary)
    {
        DialogicADPCM adpcmDecoder = new DialogicADPCM();
        List<int> decodedSamples = new List<int>();

        int pos = startingPos;
        int blockByte = 0xFF;

        while (blockByte != 0)
        {
            blockByte = combinedBinary[pos];
            int count = 0;

            while (count < blockByte)
            {
                pos++;

                byte currentByte = combinedBinary[pos];

                int upperNibble = (currentByte >> 4) & 0x0F;
                int lowerNibble = currentByte & 0x0F;

                decodedSamples.Add(adpcmDecoder.decodeSample(upperNibble));
                decodedSamples.Add(adpcmDecoder.decodeSample(lowerNibble));

                count++;
            }

            pos++;
        }

        return decodedSamples;
    }

    private static byte[] combineBinaries(List<string> inputBinaries)
    {
        List<byte> combinedBinary = new List<byte>();
        foreach (string binaryFile in inputBinaries)
        {
            if (!File.Exists(binaryFile))
            {
                Console.WriteLine($"Error: Input binary file '{binaryFile}' does not exist.");
                continue;
            }
            byte[] binaryData = File.ReadAllBytes(binaryFile);
            combinedBinary.AddRange(binaryData);
        }
        return combinedBinary.ToArray();
    }
}