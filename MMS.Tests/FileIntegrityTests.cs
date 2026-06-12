using System.Drawing;
using System.Runtime.InteropServices;
using MMS.Core.FileFormat;
using MMS.Core.FileFormat.Colorspace;
using MMS.Core.FileFormat.Compression;
using MMS.Core.FileManager;

namespace MMS.Tests;

[TestFixture]
public sealed class FileIntegrityTests
{
    private string _sourceImagePath = null!;
    private string _outputDirectory = null!;

    public static IEnumerable<TestCaseData> ColorspaceCompressionCombinations()
    {
        foreach (var colorspace in Enum.GetValues<MmsColorspace>())
        {
            foreach (var compression in Enum.GetValues<MmsCompression>())
            {
                yield return new TestCaseData(colorspace, compression).SetName($"SaveMms_{colorspace}_{compression}");
            }
        }
    }

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        var testDirectory = Path.Combine(FindRootFolder(), "test");
        _sourceImagePath = Path.Combine(testDirectory, "testImage.jpg");

        Assert.That(
            File.Exists(_sourceImagePath),
            Is.True,
            $"Required test image does not exist: {_sourceImagePath}.");

        _outputDirectory = Path.Combine(TestContext.CurrentContext.WorkDirectory, "temp");
        Directory.CreateDirectory(_outputDirectory);
    }

    [TestCaseSource(nameof(ColorspaceCompressionCombinations))]
    public void SaveMms_WithColorspaceAndCompression_ShouldHandleCombination(MmsColorspace colorspace, MmsCompression compression)
    {
        using var sourceBitmap = LoadSourceBitmap();
        var mmsFile = new MmsFile
        {
            Header = new MmsHeader
            {
                Colorspace = colorspace,
                Compression = compression
            }
        };

        if (compression == MmsCompression.Mpeg1 && colorspace != MmsColorspace.YCbCr)
        {
            Assert.That(() => mmsFile.SetBitmap(sourceBitmap), Throws.TypeOf<InvalidDataException>());
            return;
        }

        var outputPath = Path.Combine(_outputDirectory, $"{colorspace}-{compression}.mms");

        mmsFile.SetBitmap(sourceBitmap);
        new MmsFileManager().SaveImage(outputPath, mmsFile);

        Assert.Multiple(() =>
        {
            Assert.That(File.Exists(outputPath), Is.True);
            Assert.That(new FileInfo(outputPath).Length, Is.GreaterThan(0));
        });

        var loadedFile = new MmsFileManager().LoadImage(outputPath);
        using var loadedBitmap = loadedFile.GetBitmap();

        Assert.Multiple(() =>
        {
            Assert.That(loadedFile.Header.Colorspace, Is.EqualTo(colorspace));
            Assert.That(loadedFile.Header.Compression, Is.EqualTo(compression));
            Assert.That(loadedBitmap.Width, Is.EqualTo(sourceBitmap.Width));
            Assert.That(loadedBitmap.Height, Is.EqualTo(sourceBitmap.Height));
        });
    }

    [Test]
    public void LoadModifiedMms_ShouldCauseCrcMismatch()
    {
        using var sourceBitmap = LoadSourceBitmap();
        var mmsFile = new MmsFile
        {
            Header = new MmsHeader
            {
                Colorspace = MmsColorspace.Rgb,
                Compression = MmsCompression.None
            }
        };

        mmsFile.SetBitmap(sourceBitmap);

        var outputPath = Path.Combine(_outputDirectory, "crc-corrupted.mms");
        var fileManager = new MmsFileManager();
        fileManager.SaveImage(outputPath, mmsFile);

        var bytes = File.ReadAllBytes(outputPath);
        var dataOffset = Marshal.SizeOf<MmsHeader>();
        var crcOffset = bytes.Length - sizeof(uint);
        var randomByteIndex = Random.Shared.Next(dataOffset, crcOffset);

        bytes[randomByteIndex] ^= 0x01;
        File.WriteAllBytes(outputPath, bytes);

        Assert.That(() => fileManager.LoadImage(outputPath), Throws.TypeOf<InvalidDataException>());
    }

    private Bitmap LoadSourceBitmap()
    {
        return new StandardFileManager().LoadImage(_sourceImagePath).GetBitmap();
    }

    private static string FindRootFolder()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "MMS.Tests")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException();
    }
}
