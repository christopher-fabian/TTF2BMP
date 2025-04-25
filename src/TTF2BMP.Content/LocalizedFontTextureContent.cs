using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using System.Collections.Generic;

namespace TTF2BMP.Content;

/// <summary>
/// Class to allow us to add the source path to the processor.
/// </summary>
public sealed class LocalizedFontTextureContent(TextureContent baseContent, List<char> characters) : Texture2DContent
{
  public TextureContent BaseContent => baseContent;
  public List<char> Characters => characters;
}