// Builds a Penumbra mod (.pmp) that replaces game icons with PNGs.
// Usage: IconPack <png dir> <out.pmp>   PNG names are the icon id, e.g. 000123.png
using System.IO.Compression;
using System.Text.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

var pngDir = args[0];
var outPmp = args[1];
var work = Path.Combine(Path.GetTempPath(), "iconpack-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(work, "icons"));

var files = new SortedDictionary<string, string>();
foreach (var png in Directory.GetFiles(pngDir, "*.png"))
{
    if (!uint.TryParse(Path.GetFileNameWithoutExtension(png), out var id)) continue;
    var folder = id / 1000 * 1000;
    using var src = Image.Load<Rgba32>(png);
    // hr1 = high-res UI (80px), plain = standard UI (40px).
    foreach (var (suffix, size) in new[] { ("_hr1", 80), ("", 40) })
    {
        var name = $"{id:D6}{suffix}.tex";
        WriteTex(src, size, Path.Combine(work, "icons", name));
        files[$"ui/icon/{folder:D6}/{name}"] = Path.Combine("icons", name);
    }
}

File.WriteAllText(Path.Combine(work, "meta.json"), JsonSerializer.Serialize(new
{
    FileVersion = 3,
    Name = "Action Renamer Icons",
    Author = "Action Renamer",
    Description = "Sci-fi replacements for skill and PvP icons with magic circles, crosses, sparkle stars, X shapes and religious or occult symbols.",
    Version = "1.1.1",
    Website = "",
    ModTags = Array.Empty<string>(),
}, new JsonSerializerOptions { WriteIndented = true }));
File.WriteAllText(Path.Combine(work, "default_mod.json"), JsonSerializer.Serialize(new
{
    Name = "",
    Priority = 0,
    Files = files,
    FileSwaps = new Dictionary<string, string>(),
    Manipulations = Array.Empty<object>(),
}, new JsonSerializerOptions { WriteIndented = true }));

if (File.Exists(outPmp)) File.Delete(outPmp);
ZipFile.CreateFromDirectory(work, outPmp);
Directory.Delete(work, true);
Console.WriteLine($"{files.Count / 2} icons -> {outPmp}");

// Uncompressed B8G8R8A8 .tex with a full mip chain.
static void WriteTex(Image<Rgba32> src, int size, string path)
{
    var mips = new List<byte[]>();
    for (var s = size; s >= 1; s /= 2)
    {
        using var img = src.Clone(x => x.Resize(s, s, KnownResamplers.Lanczos3));
        var data = new byte[s * s * 4];
        img.CopyPixelDataTo(data);
        for (var i = 0; i < data.Length; i += 4) (data[i], data[i + 2]) = (data[i + 2], data[i]); // RGBA -> BGRA
        mips.Add(data);
        if (s == 1) break;
    }

    using var w = new BinaryWriter(File.Create(path));
    w.Write(0x00800000u);          // attribute: 2D texture
    w.Write(0x1450u);              // format: B8G8R8A8
    w.Write((ushort)size);
    w.Write((ushort)size);
    w.Write((ushort)1);            // depth
    w.Write((byte)mips.Count);     // mip levels
    w.Write((byte)0);              // array size
    w.Write(0u); w.Write(1u); w.Write(2u); // LoD offsets
    var offset = 80u;
    for (var i = 0; i < 13; i++)
    {
        w.Write(i < mips.Count ? offset : 0u);
        if (i < mips.Count) offset += (uint)mips[i].Length;
    }
    foreach (var m in mips) w.Write(m);
}
