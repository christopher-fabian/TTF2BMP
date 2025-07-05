using System.Drawing;
using System.IO;

namespace TTF2BMP.Fonts;

internal sealed class CustomFont(string filePath, FontFamily fontFamily)
{
  public string FilePath => filePath;
  public FontFamily FontFamily => fontFamily;
  public string FontName { get; } = Path.GetFileNameWithoutExtension(filePath);
}