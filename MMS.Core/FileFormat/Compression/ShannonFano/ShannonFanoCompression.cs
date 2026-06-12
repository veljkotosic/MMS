using MMS.Core.Utility;

namespace MMS.Core.FileFormat.Compression.ShannonFano;

public class ShannonFanoCompression :
    IMmsCompression
{
    public (byte[] Data, byte[] Metadata) Compress(byte[] data, int width, int height, int channels)
    {
        if (data.Length == 0)
        {
            return ([], []);
        }

        var frequencies = data
            .GroupBy(b => b)
            .Select(g => new Symbol { Value = g.Key, Frequency = g.Count() })
            .OrderByDescending(s => s.Frequency)
            .ToList();

        var codes = new Dictionary<byte, string>();
        GenerateCodes(frequencies, "", codes);

        using var metadataStream = new MemoryStream();
        using var metadataWriter = new BinaryWriter(metadataStream);
        
        metadataWriter.Write((ushort)codes.Count);
        
        foreach (var kvp in codes)
        {
            metadataWriter.Write(kvp.Key);
            metadataWriter.Write((byte)kvp.Value.Length);
            metadataWriter.Write(kvp.Value); 
        }

        using var dataStream = new MemoryStream();
        using var dataWriter = new BinaryWriter(dataStream);
        
        dataWriter.Write(data.Length); 

        byte currentByte = 0;
        int bitCount = 0;

        foreach (byte b in data)
        {
            var code = codes[b];
            
            foreach (var bit in code)
            {
                currentByte <<= 1;
                
                if (bit == '1')
                {
                    currentByte |= 1;
                }
                
                bitCount++;

                if (bitCount == 8)
                {
                    dataWriter.Write(currentByte);
                    currentByte = 0;
                    bitCount = 0;
                }
            }
        }

        if (bitCount > 0)
        {
            currentByte <<= (8 - bitCount);
            dataWriter.Write(currentByte);
        }

        return (dataStream.ToArray(), metadataStream.ToArray());
    }

    public byte[] Decompress(byte[] data, byte[] metadata, int width, int height, int channels)
    {
        if (data.Length == 0) return [];

        using var metadataStream = new MemoryStream(metadata);
        using var metadataReader = new BinaryReader(metadataStream);
        
        int dictionarySize = metadataReader.ReadUInt16();
        
        var root = new ShannonFanoTreeNode();
        var lookUpTable = new LookUpTableEntry[256]; 

        for (int i = 0; i < dictionarySize; i++)
        {
            var value = metadataReader.ReadByte();
            var codeLen = metadataReader.ReadByte();
            var code = metadataReader.ReadString();

            var current = root;
            foreach (var bit in code)
            {
                if (bit == '0')
                {
                    current.Left ??= new ShannonFanoTreeNode();
                    current = current.Left;
                }
                else
                {
                    current.Right ??= new ShannonFanoTreeNode();
                    current = current.Right;
                }
            }
            current.Value = value;

            if (codeLen <= 8)
            {
                var numericCode = 0;
                foreach (var bit in code)
                {
                    numericCode = (numericCode << 1) | (bit == '1' ? 1 : 0);
                }

                var remainingBits = 8 - codeLen;
                var start = numericCode << remainingBits;
                var end = start + (1 << remainingBits);

                for (int j = start; j < end; j++)
                {
                    lookUpTable[j] = new LookUpTableEntry
                    {
                        Value = value,
                        Length = codeLen,
                        IsValid = true
                    };
                }
            }
        }

        using var dataStream = new MemoryStream(data);
        using var dataReader = new BinaryReader(dataStream);
        
        var originalLength = dataReader.ReadInt32();
        if (originalLength < 0 || originalLength > ApplicationLimits.MaxDecodedImageBytes)
        {
            throw new InvalidDataException("Invalid data length.");
        }

        var result = new byte[originalLength];
        var resultIndex = 0;

        uint bitBuffer = 0;
        var bitsInDrawingBuffer = 0;

        while (resultIndex < originalLength)
        {
            while (bitsInDrawingBuffer < 16 && dataStream.Position < dataStream.Length)
            {
                bitBuffer = (bitBuffer << 8) | dataReader.ReadByte();
                bitsInDrawingBuffer += 8;
            }

            var peek = (int)((bitBuffer >> (bitsInDrawingBuffer - 8)) & 0xFF);
            var entry = lookUpTable[peek];

            if (entry.IsValid)
            {
                result[resultIndex++] = entry.Value;
                bitsInDrawingBuffer -= entry.Length;
            }
            else
            {
                var currentNode = root;
                
                while (!currentNode.Value.HasValue)
                {
                    if (bitsInDrawingBuffer == 0 && dataStream.Position < dataStream.Length)
                    {
                        bitBuffer = (bitBuffer << 8) | dataReader.ReadByte();
                        bitsInDrawingBuffer += 8;
                    }

                    var bit = (int)((bitBuffer >> (bitsInDrawingBuffer - 1)) & 1);
                    bitsInDrawingBuffer--;
                    
                    currentNode = (bit == 0) ? currentNode.Left! : currentNode.Right!;
                }
                
                result[resultIndex++] = currentNode.Value.Value;
            }
        }

        return result;
    }

    private static void GenerateCodes(List<Symbol> symbols, string prefix, Dictionary<byte, string> codes)
    {
        if (symbols.Count == 1)
        {
            codes[symbols[0].Value] = string.IsNullOrEmpty(prefix) ? "0" : prefix;
            return;
        }

        var splitIndex = FindSplitIndex(symbols);

        GenerateCodes(symbols.GetRange(0, splitIndex), prefix + "0", codes);
        GenerateCodes(symbols.GetRange(splitIndex, symbols.Count - splitIndex), prefix + "1", codes);
    }

    private static int FindSplitIndex(List<Symbol> symbols)
    {
        var total = symbols.Sum(s => (long)s.Frequency);
        long currentSum = 0;
        var minDifference = total;
        var splitIndex = 1;

        for (int i = 0; i < symbols.Count - 1; i++)
        {
            currentSum += symbols[i].Frequency;
            var difference = Math.Abs(total - currentSum - currentSum);
            
            if (difference < minDifference)
            {
                minDifference = difference;
                splitIndex = i + 1;
            }
        }
        
        return splitIndex;
    }
}
