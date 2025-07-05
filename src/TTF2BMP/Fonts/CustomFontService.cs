using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Drawing.Text;
using System.IO;

namespace TTF2BMP.Fonts;

internal sealed class CustomFontService
{
  private readonly Dictionary<string, CustomFont> customFonts = [];
  private readonly PrivateFontCollection customFontCollection = new();

  public IEnumerable<CustomFont> Fonts => customFonts.Values;

  public IEnumerable<CustomFont> Load(IEnumerable<string> filePaths)
  {
    ArgumentNullException.ThrowIfNull(filePaths, nameof(filePaths));

    List<CustomFont> customFonts = [];

    foreach (string filePath in filePaths)
    {
      if (TryLoad(filePath, out CustomFont? customFont))
        customFonts.Add(customFont);
    }

    return customFonts;
  }

  public bool TryLoad(string filePath, [NotNullWhen(true)] out CustomFont? customFont)
  {
    ArgumentException.ThrowIfNullOrEmpty(filePath, nameof(filePath));

    if (!File.Exists(filePath))
    {
      customFont = null;
      return false;
    }

    customFontCollection.AddFontFile(filePath);

    FontFamily fontFamily = customFontCollection.Families[^1];

    customFont = new(filePath, fontFamily);
    customFonts.Add(customFont.FontName, customFont);
    return true;
  }

  public bool TryGetFontFamily(string customFontName, [NotNullWhen(true)] out FontFamily? fontFamily)
  {
    if (customFonts.TryGetValue(customFontName, out CustomFont? customFont))
    {
      fontFamily = customFont.FontFamily;
      return true;
    }

    fontFamily = null;
    return false;
  }
}