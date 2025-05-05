using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Windows.Forms;
using TTF2BMP.Properties;

namespace TTF2BMP;

public partial class MainForm : Form
{
  // SpriteFont Importer only expects characters from the ranges defined in <CharacterRegions>
  // (typically from Space (32) to Tilde (126)).
  private const int FirstCharacterUnicode = 32; // Space
  private const int LastCharacterUnicode = 126; // Tilde

  private readonly PrivateFontCollection customFontCollection = new();
  private readonly Dictionary<string, FontFamily> customFontFamilies = [];
  private readonly Bitmap globalBitmap;
  private readonly Graphics globalGraphics;
  private Font? selectedFont;
  private string? fontError;

  public MainForm()
  {
    InitializeComponent();

    globalBitmap = new(1, 1, PixelFormat.Format32bppArgb);
    globalGraphics = Graphics.FromImage(globalBitmap);

    foreach (FontFamily fontFamily in FontFamily.Families)
      comboBoxFontName.Items.Add(fontFamily.Name);

    comboBoxFontName.Text = Settings.Default.FontName;
    OutlineColorSample.BackColor = Settings.Default.OutlineColor;
    ShadowColorSample.BackColor = Settings.Default.ShadowColor;

    if (Settings.Default.TextFiles is not null)
    {
      foreach (string? fileName in Settings.Default.TextFiles)
      {
        if (fileName is not null)
          TextFilesListBox.Items.Add(fileName);
      }
    }
  }

  /// <summary>
  /// When the font selection changes, create a new Font
  /// instance and update the preview text label.
  /// </summary>
  private void SelectionChanged()
  {
    try
    {
      // Parse the font size selection
      if (!float.TryParse(comboBoxFontSize.Text, out float size) || (size <= 0))
      {
        fontError = $"Invalid font size '{comboBoxFontSize.Text}'";
        return;
      }

      // Parse the font style selection
      if (!Enum.TryParse(comboBoxFontStyle.Text, out FontStyle style))
      {
        fontError = $"Invalid font style '{comboBoxFontStyle.Text}'";
        return;
      }

      string fontFamilyName = comboBoxFontName.Text;
      Font font = customFontFamilies.TryGetValue(fontFamilyName, out FontFamily? fontFamily)
        ? new(fontFamily, size, style)
        : new(fontFamilyName, size, style);

      selectedFont?.Dispose();

      labelSampleText.Font = selectedFont = font;

      fontError = null;
    }
    catch (Exception ex)
    {
      fontError = ex.Message;
    }
  }

  /// <summary>
  /// Helper for rendering out a single font character
  /// into a System.Drawing bitmap.
  /// </summary>
  private Bitmap RasterizeCharacter(char character)
  {
    string text = character.ToString();

    SizeF size = globalGraphics.MeasureString(text, selectedFont);
    int width = (int)Math.Ceiling(size.Width);
    int height = (int)Math.Ceiling(size.Height);

    Bitmap bitmap = new(width, height, PixelFormat.Format32bppArgb);

    using (Graphics graphics = Graphics.FromImage(bitmap))
    {
      graphics.TextRenderingHint = Antialias.Checked ? TextRenderingHint.ClearTypeGridFit : TextRenderingHint.SingleBitPerPixelGridFit;
      graphics.Clear(Color.Transparent);

      int alpha = int.Parse(AlphaAmount.Text);
      if (alpha < 0)
        alpha = 0;
      if (alpha > 255)
        alpha = 255;
      Color customColor = Color.FromArgb(alpha, ShadowColorSample.BackColor);
      using (Brush brush = new SolidBrush(Color.White))
      using (Brush brushOutline = new SolidBrush(OutlineColorSample.BackColor))
      using (Brush brushShadow = new SolidBrush(customColor))
      using (StringFormat format = new())
      {
        format.Alignment = StringAlignment.Near;
        format.LineAlignment = StringAlignment.Near;

        int shadow = int.Parse(ShadowOffset.Text);
        int outline = int.Parse(OutlineSize.Text);

        // Draw the shadow
        if (shadow > 0)
          graphics.DrawString(text, selectedFont, brushShadow, shadow, shadow, format);

        // Draw the outline
        if (outline > 0)
        {
          for (int i = 1; i <= outline; ++i)
          {
            graphics.DrawString(text, selectedFont, brushOutline, -1 * i, -1 * i, format);
            graphics.DrawString(text, selectedFont, brushOutline, 0, -1 * i, format);
            graphics.DrawString(text, selectedFont, brushOutline, 1 * i, -1 * i, format);
            graphics.DrawString(text, selectedFont, brushOutline, -1 * i, 0, format);
            graphics.DrawString(text, selectedFont, brushOutline, 1 * i, 0, format);
            graphics.DrawString(text, selectedFont, brushOutline, -1 * i, 1 * i, format);
            graphics.DrawString(text, selectedFont, brushOutline, 0, 1 * i, format);
            graphics.DrawString(text, selectedFont, brushOutline, 1 * i, 1 * i, format);
          }
        }

        // Draw the text
        graphics.DrawString(text, selectedFont, brush, 0, 0, format);
      }

      graphics.Flush();
    }

    return CropCharacter(bitmap);
  }

  /// <summary>
  /// Helper for cropping ununsed space from the sides of a bitmap.
  /// </summary>
  private static Bitmap CropCharacter(Bitmap bitmap)
  {
    int cropLeft = 0;
    int cropRight = bitmap.Width - 1;

    // Remove unused space from the left.
    while ((cropLeft < cropRight) && (BitmapIsEmpty(bitmap, cropLeft)))
      cropLeft++;

    //If the entire glyph is blank, output the full blank glyph.
    if (cropLeft == cropRight)
    {
      return bitmap;
    }

    // Remove unused space from the right.
    while ((cropRight > cropLeft) && (BitmapIsEmpty(bitmap, cropRight)))
      cropRight--;

    // Don't crop if that would reduce the glyph down to nothing at all!
    if (cropLeft > cropRight) // Note:  cropRight is inclusive, so for letter's like I, l, |, etc, cropLeft == cropRight.
      return bitmap;

    // Add some padding back in.
    cropLeft = Math.Max(cropLeft - 1, 0);
    cropRight = Math.Min(cropRight + 1, bitmap.Width - 1);

    int width = cropRight - cropLeft + 1;

    // Crop the glyph.
    Bitmap croppedBitmap = new(width, bitmap.Height, bitmap.PixelFormat);

    using (Graphics graphics = Graphics.FromImage(croppedBitmap))
    {
      graphics.CompositingMode = CompositingMode.SourceCopy;
      graphics.DrawImage(bitmap, 0, 0, new(cropLeft, 0, width, bitmap.Height), GraphicsUnit.Pixel);
      graphics.Flush();
    }

    bitmap.Dispose();

    return croppedBitmap;
  }

  /// <summary>
  /// Helper for testing whether a column of a bitmap is entirely empty.
  /// </summary>
  private static bool BitmapIsEmpty(Bitmap bitmap, int x)
  {
    for (int y = 0; y < bitmap.Height; y++)
    {
      if (bitmap.GetPixel(x, y).A != 0)
        return false;
    }

    return true;
  }

  private void FontName_SelectedIndexChanged(object sender, EventArgs e)
  {
    SelectionChanged();
  }

  private void FontStyle_SelectedIndexChanged(object sender, EventArgs e)
  {
    SelectionChanged();
  }

  private void FontSize_TextUpdate(object sender, EventArgs e)
  {
    SelectionChanged();
  }

  private void FontSize_SelectedIndexChanged(object sender, EventArgs e)
  {
    SelectionChanged();
  }

  private void ChooseTextFilesButton_Click(object sender, EventArgs e)
  {
    // Choose the files to read text from
    OpenFileDialog openFileDialog = new()
    {
      InitialDirectory = Settings.Default.TextFilesDir,
      Title = "Coose Text Files",
      DefaultExt = "*",
      Filter = "All files (*.*)|*.*",
      Multiselect = true
    };

    if (openFileDialog.ShowDialog() == DialogResult.OK)
    {
      TextFilesListBox.Items.Clear();
      foreach (string fileName in openFileDialog.FileNames)
        TextFilesListBox.Items.Add(fileName);

      Settings.Default.TextFilesDir = Path.GetDirectoryName(openFileDialog.FileNames[0]);
    }
  }

  private void Export_Click(object sender, EventArgs e)
  {
    try
    {
      // If the current font is invalid, report that to the user.
      if (fontError is not null)
        throw new ArgumentException(fontError);

      // Choose the output file.
      SaveFileDialog fileSelector = new()
      {
        InitialDirectory = Settings.Default.ExportDir,
        Title = "Export Font",
        DefaultExt = "bmp",
        Filter = "Image files (*.bmp)|*.bmp|All files (*.*)|*.*"
      };

      if (fileSelector.ShowDialog() == DialogResult.OK)
      {
        // Get the path to the game text
        string outputDir = Path.GetDirectoryName(fileSelector.FileName)!;
        Settings.Default.ExportDir = outputDir;

        // Just grab every string in every language.
        string allText = string.Empty;

        HashSet<char> characterSet = [];

        if (checkBoxExportDefault.Checked)
        {
          // Export all glyphs in the font.
          for (int i = FirstCharacterUnicode; i < LastCharacterUnicode; i++)
            characterSet.Add((char)i);
        }
        else if (TextFilesListBox.Items.Count > 0)
        {
          foreach (string file in TextFilesListBox.Items)
          {
            string absolutePath = Path.GetFullPath(file);
            string readText = File.ReadAllText(file);
            allText += readText;
          }
        }

        // Scan each character of the string.
        foreach (char usedCharacter in allText)
        {
          if (usedCharacter < FirstCharacterUnicode || usedCharacter > LastCharacterUnicode)
            continue;

          characterSet.Add(usedCharacter);
        }

        // Character map must be in ascending order.
        List<char> characters = [.. characterSet];
        characters.Sort();

        // Build up a list of all the glyphs to be output.
        List<Bitmap> bitmaps = [];
        List<int> xPositions = [];
        List<int> yPositions = [];

        try
        {
          const int padding = 8;

          int width = padding;
          int height = padding;
          int lineWidth = padding;
          int lineHeight = padding;
          int count = 0;

          // Rasterize each character in turn,
          // and add it to the output list.
          foreach (char character in characters)
          {
            Bitmap bitmap = RasterizeCharacter(character);
            bitmaps.Add(bitmap);

            xPositions.Add(lineWidth);
            yPositions.Add(height);

            lineWidth += bitmap.Width + padding;
            lineHeight = Math.Max(lineHeight, bitmap.Height + padding);

            // Output 16 glyphs per line, then wrap to the next line.
            if (++count == 16)
            {
              width = Math.Max(width, lineWidth);
              height += lineHeight;
              lineWidth = padding;
              lineHeight = padding;
              count = 0;
            }
          }

          using (Bitmap bitmap = new(width, height + lineHeight, PixelFormat.Format32bppArgb))
          {
            // Arrage all the glyphs onto a single larger bitmap.
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
              graphics.Clear(Color.Magenta);
              graphics.CompositingMode = CompositingMode.SourceCopy;

              for (int i = 0; i < bitmaps.Count; i++)
                graphics.DrawImage(bitmaps[i], xPositions[i], yPositions[i]);

              graphics.Flush();
            }

            // Save out the combined bitmap.
            bitmap.Save(fileSelector.FileName, ImageFormat.Bmp);
          }
        }
        finally
        {
          // Clean up temporary objects.
          foreach (Bitmap bitmap in bitmaps)
            bitmap.Dispose();
        }

        // Output the characters we just rendered to a text file
        string charOutput = Path.ChangeExtension(fileSelector.FileName, ".txt");
        using (StreamWriter writer = new(charOutput))
        {
          for (int i = 0; i < characters.Count; i++)
            writer.Write(characters[i]);
        }
      }
    }
    catch (Exception exception)
    {
      // Report any errors to the user.
      MessageBox.Show(exception.Message, Text + " Error");
    }
  }

  private void OutlineColorSample_Click(object sender, EventArgs e)
  {
    colorDialog.Color = OutlineColorSample.BackColor;
    colorDialog.ShowDialog();
    OutlineColorSample.BackColor = colorDialog.Color;
  }

  private void ShadowColorSample_Click(object sender, EventArgs e)
  {
    colorDialog.Color = ShadowColorSample.BackColor;
    colorDialog.ShowDialog();
    ShadowColorSample.BackColor = colorDialog.Color;
  }

  private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
  {
    Settings.Default.TextFiles = [];

    foreach (string fileName in TextFilesListBox.Items)
      Settings.Default.TextFiles.Add(fileName);

    Settings.Default.Save();
  }

  private void CheckBoxExportDefault_CheckedChanged(object sender, EventArgs e)
  {
    TextFilesListBox.Enabled = ChooseTextFilesButton.Enabled = !checkBoxExportDefault.Checked;
  }

  private void ButtonChooseFontFiles_Click(object sender, EventArgs e)
  {
    OpenFileDialog openFileDialog = new()
    {
      InitialDirectory = Settings.Default.TextFilesDir,
      Title = "Coose Font Files",
      DefaultExt = "*",
      Filter = "All files (*.*)|*.*",
      Multiselect = true
    };

    if (openFileDialog.ShowDialog() == DialogResult.OK)
    {
      foreach (string fileName in openFileDialog.FileNames)
      {
        string fontName = Path.GetFileNameWithoutExtension(fileName);

        comboBoxFontName.Items.Add(fontName);

        customFontCollection.AddFontFile(fileName);
        customFontFamilies.Add(fontName, customFontCollection.Families[^1]);
      }
    }
  }
}