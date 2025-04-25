using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using System.Collections.Generic;
using System.ComponentModel;

namespace TTF2BMP.Content;

[ContentProcessor(DisplayName = "Font Texture - Localized")]
public sealed class LocalizedFontTextureProcessor : FontTextureProcessor
{
  private List<char> characters = default!;

  [DefaultValue('?')]
  public char DefaultCharacter { get; set; }

  /// <summary>
  /// Creates a new LocalizedFontTextureProcessor.
  /// </summary>
  public LocalizedFontTextureProcessor() : base()
    => DefaultCharacter = '?';

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
    SpriteFontContent outputContent = base.Process(textureContent, context);
    // Need to set a default character for this kind of font as well
    outputContent.DefaultCharacter = DefaultCharacter;

    return outputContent;
  }
}