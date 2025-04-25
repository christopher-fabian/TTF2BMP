using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections.Generic;
using System.IO;

namespace TTF2BMP.Content;

/// <summary>
/// Class to import a text file of chars along with the bitmap they reference
/// </summary>
[ContentImporter(".bmp", DefaultProcessor = "LocalizedFontTextureProcessor", DisplayName = "Texture Importer - Localized Font")]
public sealed class LocalizedFontTextureImporter : TextureImporter
{
  public override TextureContent Import(string filename, ContentImporterContext context)
  {
    // Use the base MonoGame Texture Importer
    TextureContent baseTextureContent = base.Import(filename, context);

    // Get the text file that should have been generated with the same name as the bitmap
    string textFileName = Path.ChangeExtension(filename, ".txt");

    // Read all the characters
    string allChars = File.ReadAllText(textFileName);
    List<char> chars = [.. allChars];
    LocalizedFontTextureContent localizedFontTextureContent = new(baseTextureContent, chars);

    return localizedFontTextureContent;
  }
}