using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System.Collections.Generic;
using System.ComponentModel;

namespace TTF2BMP.Content;

[ContentProcessor(DisplayName = "Localized Font - Processor")]
public sealed class LocalizedFontTextureProcessor : FontTextureProcessor
{
  private List<char> characters = [];

  [DefaultValue('?')]
  public char DefaultCharacter { get; set; } = '?';

  /// <inheritdoc/>
  protected override char GetCharacterForIndex(int index)
    => characters[index];

  /// <inheritdoc/>
  public override SpriteFontContent Process(Texture2DContent input, ContentProcessorContext context)
  {
    // Cast the input to our own special format
    LocalizedFontTextureContent localizedContent = (LocalizedFontTextureContent)input;
    characters = localizedContent.Characters;

    // Our localized texture input just contains the base Texture2DContent and the list of characters
    Texture2DContent textureContent = (Texture2DContent)localizedContent.BaseContent;
    SpriteFontContent spriteFontContent = base.Process(textureContent, context);
    // Set the default character for this kind of font as well
    spriteFontContent.DefaultCharacter = DefaultCharacter;

    return spriteFontContent;
  }
}