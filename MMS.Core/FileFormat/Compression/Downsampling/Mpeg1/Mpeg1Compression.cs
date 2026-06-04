using MMS.Core.FileFormat.Compression.ShannonFano;

namespace MMS.Core.FileFormat.Compression.Downsampling.Mpeg1;

public class Mpeg1Compression :
    IMmsCompression
{
    private static readonly double[,] CosTable = InitializeCosTable();
    
    private static double[,] InitializeCosTable()
    {
        var table = new double[8, 8];
        
        for (int u = 0; u < 8; u++)
        {
            for (int i = 0; i < 8; i++)
            {
                table[u, i] = Math.Cos((2 * i + 1) * u * Math.PI / 16.0);
            }
        }
        
        return table;
    }
    
    private static readonly int[] ZigZagMap =
    [
        0,  1,  8,  16, 9,  2,  3,  10,
        17, 24, 32, 25, 18, 11, 4,  5,
        12, 19, 26, 33, 40, 48, 41, 34,
        27, 20, 13, 6,  7,  14, 21, 28,
        35, 42, 49, 56, 57, 50, 43, 36,
        29, 22, 15, 23, 30, 37, 44, 51,
        58, 59, 52, 45, 38, 31, 39, 46,
        53, 60, 61, 54, 47, 55, 62, 63
    ];

    public (byte[] Data, byte[] Metadata) Compress(byte[] data, int width, int height, int channels)
    {
        var (yPlane, cbPlane, crPlane) = Downsample420(data, width, height, channels);

        List<byte> processedData = [];
        
        ProcessPlane(yPlane, width, height, QuantizationTables.Luma, processedData);
        ProcessPlane(cbPlane, width / 2, height / 2, QuantizationTables.Chroma, processedData);
        ProcessPlane(crPlane, width / 2, height / 2, QuantizationTables.Chroma, processedData);

        var shannonFano = new ShannonFanoCompression();
        
        return shannonFano.Compress(processedData.ToArray(), width, height, channels);
    }

    public byte[] Decompress(byte[] data, byte[] metadata, int width, int height, int channels)
    {
        var shannonFano = new ShannonFanoCompression();
        
        var decompressedBlocks = shannonFano.Decompress(data, metadata, width, height, channels);

        var blockIdx = 0;
        
        var yPlane = DeprocessPlane(decompressedBlocks, ref blockIdx, width, height, QuantizationTables.Luma);
        var cbSub = DeprocessPlane(decompressedBlocks, ref blockIdx, width / 2, height / 2, QuantizationTables.Chroma);
        var crSub = DeprocessPlane(decompressedBlocks, ref blockIdx, width / 2, height / 2, QuantizationTables.Chroma);

        return Upsample420(yPlane, cbSub, crSub, width, height, channels);
    }

    private static (byte[] Y, byte[] Cb, byte[] Cr) Downsample420(byte[] data, int width, int height, int channels)
    {
        var yPlane = new byte[width * height];
        var cbPlane = new byte[(width / 2) * (height / 2)];
        var crPlane = new byte[(width / 2) * (height / 2)];

        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                var index = (j * width + i) * channels;
                yPlane[j * width + i] = data[index];

                if (j % 2 == 0 && i % 2 == 0 && (j / 2) < (height / 2) && (i / 2) < (width / 2))
                {
                    int sumCb = 0, sumCr = 0;
                    int count = 0;
                    
                    for (int dy = 0; dy < 2; dy++)
                    {
                        for (int dx = 0; dx < 2; dx++)
                        {
                            var yPos = j + dy;
                            var xPos = i + dx;
                            
                            if (yPos < height && xPos < width)
                            {
                                int nIndex = (yPos * width + xPos) * channels;
                                
                                sumCb += data[nIndex + 1];
                                sumCr += data[nIndex + 2];
                                count++;
                            }
                        }
                    }
                    
                    cbPlane[j / 2 * (width / 2) + i / 2] = (byte)(sumCb / count);
                    crPlane[j / 2 * (width / 2) + i / 2] = (byte)(sumCr / count);
                }
            }
        }
        
        return (yPlane, cbPlane, crPlane);
    }

    private static byte[] Upsample420(byte[] yPlane, byte[] cbPlane, byte[] crPlane, int width, int height, int channels)
    {
        var result = new byte[width * height * channels];
        
        for (int j = 0; j < height; j++)
        {
            for (int i = 0; i < width; i++)
            {
                int rIndex = (j * width + i) * channels;
                
                result[rIndex] = yPlane[j * width + i];
                result[rIndex + 1] = cbPlane[j / 2 * (width / 2) + i / 2];
                result[rIndex + 2] = crPlane[j / 2 * (width / 2) + i / 2];
                
                if (channels == 4)
                {
                    result[rIndex + 3] = 255;
                }
            }
        }
        
        return result;
    }

    private static void ProcessPlane(byte[] plane, int width, int height, byte[] quantizationTable, List<byte> output)
    {
        for (int y = 0; y < height; y += 8)
        {
            for (int x = 0; x < width; x += 8)
            {
                var block = GetBlock(plane, x, y, width, height);
                var dct = CalculateDct(block);
                var quantized = Quantize(dct, quantizationTable);
                
                foreach (var mapIndex in ZigZagMap)
                {
                    output.Add((byte)Math.Clamp(quantized[mapIndex] + 128, 0, 255));
                }
            }
        }
    }

    private static byte[] DeprocessPlane(byte[] data, ref int startIndex, int width, int height, byte[] quantizationTable)
    {
        var plane = new byte[width * height];
        
        for (int y = 0; y < height; y += 8)
        {
            for (int x = 0; x < width; x += 8)
            {
                var quantized = new int[64];
                
                for (int i = 0; i < 64; i++)
                {
                    quantized[i] = data[startIndex++] - 128;
                }

                var zigZagged = new int[64];
                
                for (int i = 0; i < 64; i++)
                {
                    zigZagged[ZigZagMap[i]] = quantized[i];
                }

                var dequantized = Dequantize(zigZagged, quantizationTable);
                var inverseDct = CalculateInverseDct(dequantized);
                
                SetBlock(plane, inverseDct, x, y, width, height);
            }
        }
        
        return plane;
    }

    private static double[,] GetBlock(byte[] plane, int x, int y, int width, int height)
    {
        var block = new double[8, 8];
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                var pixelY = y + i;
                var pixelX = x + j;

                block[i, j] = pixelX < width && pixelY < height ? plane[pixelY * width + pixelX] : 0;
            }
        }
        
        return block;
    }

    private static void SetBlock(byte[] plane, double[,] block, int x, int y, int width, int height)
    {
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                if (x + j < width && y + i < height)
                {
                    plane[(y + i) * width + x + j] = (byte)Math.Clamp(block[i, j], 0, 255);
                }            
            }
        }
    }

    private const double Sqrt2O2 = 0.7071067811865476; 
    
    private static double[,] CalculateDct(double[,] block)
    {
        var dct = new double[8, 8];
        
        for (int u = 0; u < 8; u++)
        {
            for (int v = 0; v < 8; v++)
            {
                double sum = 0;
                
                for (int i = 0; i < 8; i++)
                {
                    var cosUi = CosTable[u, i];
                    
                    for (int j = 0; j < 8; j++)
                    {
                        sum += block[i, j] * cosUi * CosTable[v, j];
                    }
                }    
                
                var alphaU = (u == 0) ? Sqrt2O2 : 1.0;
                var alphaV = (v == 0) ? Sqrt2O2 : 1.0;
                
                dct[u, v] = 0.25 * alphaU * alphaV * sum;
            }
        }
        return dct;
    }

    private static double[,] CalculateInverseDct(double[,] dct)
    {
        var block = new double[8, 8];
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                double sum = 0;
                
                for (int u = 0; u < 8; u++)
                {
                    var alphaU = (u == 0) ? Sqrt2O2 : 1.0;
                    var cosUi = CosTable[u, i];
                    
                    for (int v = 0; v < 8; v++)
                    {
                        var alphaV = (v == 0) ? Sqrt2O2 : 1.0;
                        sum += alphaU * alphaV * dct[u, v] * cosUi * CosTable[v, j];
                    }
                }
                
                block[i, j] = 0.25 * sum;
            }
        }
        
        return block;
    }

    private static int[] Quantize(double[,] dct, byte[] table)
    {
        var quantized = new int[64];
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                quantized[i * 8 + j] = (int)Math.Round(dct[i, j] / table[i * 8 + j]);
            }
        }
        
        return quantized;
    }

    private static double[,] Dequantize(int[] q, byte[] table)
    {
        var dct = new double[8, 8];
        
        for (int i = 0; i < 8; i++)
        {
            for (int j = 0; j < 8; j++)
            {
                dct[i, j] = q[i * 8 + j] * table[i * 8 + j];
            }
        }
        
        return dct;
    }
}