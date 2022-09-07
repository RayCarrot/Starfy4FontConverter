using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using BinarySerializer;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;

namespace Starfy4FontConverter;

public partial class ConverterViewModel : ObservableObject
{
    #region Private Constant Fields

    private const string BinaryFontFileExt = ".bin";
    private const string BinaryFontFileType = "Binary font files";
    private const string ConvertedFontSheetFileExt = ".dat";
    private const string ConvertedFontSheetFileType = "Font sheet files";

    #endregion

    #region Private Helper Methods

    // TODO: Move private helper methods to other classes
    private string? RequestOpenFilePath(string title, string filter)
    {
        OpenFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
        };

        bool? result = dialog.ShowDialog();

        if (result != true)
            return null;

        return dialog.FileName;
    }

    private string? RequestSaveFilePath(string title, string filter, string defaultFilePath)
    {
        SaveFileDialog dialog = new()
        {
            Title = title,
            Filter = filter,
            FileName = defaultFilePath,
        };

        bool? result = dialog.ShowDialog();

        if (result != true)
            return null;

        return dialog.FileName;
    }

    private string GetFileFilter(string ext, string fileType) => $"{fileType} (*{ext})|*{ext}";

    private void ShowException(Exception ex) => 
        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

    private void DisplaySuccessMessage(string message) => 
        MessageBox.Show(message, "Success", MessageBoxButton.OK, MessageBoxImage.Information);

    #endregion

    #region Public Command Methods

    [RelayCommand]
    public void ConvertBinToSheet()
    {
        string inputFilter = GetFileFilter(BinaryFontFileExt, BinaryFontFileType);
        string? inputFilePath = RequestOpenFilePath("Select the binary font file", inputFilter);

        if (inputFilePath is null)
            return;

        string outputFilter = GetFileFilter(ConvertedFontSheetFileExt, ConvertedFontSheetFileType);
        string? outputFilePath = RequestSaveFilePath("Select the destination for the converted font sheet", outputFilter, 
            Path.ChangeExtension(Path.GetFileName(inputFilePath), ConvertedFontSheetFileExt));

        if (outputFilePath is null)
            return;

        try
        {
            using Context context = new(String.Empty);
            context.AddFile(new LinearFile(context, inputFilePath));
            Starfy4Font font = FileFactory.Read<Starfy4Font>(context, inputFilePath);

            File.WriteAllBytes(outputFilePath, 
                font.FontSprites.SelectMany(x => x?.Value?.ImgData ?? Enumerable.Empty<byte>()).ToArray());

            DisplaySuccessMessage("Font successfully converted");
        }
        catch (Exception ex)
        {
            ShowException(ex);
        }
    }

    [RelayCommand]
    public void ImportSheetToBin()
    {
        string inputFilter = GetFileFilter(ConvertedFontSheetFileExt, ConvertedFontSheetFileType);
        string? inputFilePath = RequestOpenFilePath("Select the converted font sheet", inputFilter);

        if (inputFilePath is null)
            return;

        string outputFilter = GetFileFilter(BinaryFontFileExt, BinaryFontFileType);
        string? outputFilePath = RequestSaveFilePath("Select the binary font file to import to", outputFilter, 
            Path.ChangeExtension(Path.GetFileName(inputFilePath), BinaryFontFileExt));

        if (outputFilePath is null)
            return;

        try
        {
            using Context context = new(String.Empty);
            context.AddFile(new LinearFile(context, outputFilePath));
            Starfy4Font font = FileFactory.Read<Starfy4Font>(context, outputFilePath);

            // Hack for checking if it's the small font
            bool isSmallFont = font.FontSprites.First(x => x?.Value is not null)?.Value.Byte_00 == 8;
            
            byte[] fontSheet = File.ReadAllBytes(inputFilePath);

            int existingFontSpritesCount = font.FontSprites.Count(x => x?.Value is not null);

            // Verify size
            int spriteSize = isSmallFont ? 26 : 52;
            int expectedSize = existingFontSpritesCount * spriteSize;
            if (fontSheet.Length != expectedSize)
                throw new Exception($"Font sheet size {fontSheet.Length} does not match expected size of {expectedSize}");

            // Create a sprite for every font character
            List<Pointer<Starfy4FontSprite>?> fontSprites = new();
            Pointer currentPointer = new(2 + 2 + 4 * font.FontSpritesCount, font.Offset.File);

            int fontsheetSpriteIndex = 0;
            for (int i = 0; i < font.FontSpritesCount; i++)
            {
                if (font.FontSprites[i]?.Value == null)
                {
                    fontSprites.Add(null);
                }
                else
                {
                    byte[] imgData = new byte[spriteSize];
                    Array.Copy(fontSheet, fontsheetSpriteIndex * spriteSize, imgData, 0, spriteSize);
                    fontsheetSpriteIndex++;

                    // Check for matching sprite
                    Pointer<Starfy4FontSprite>? sprite = fontSprites.FirstOrDefault(x => x?.Value?.ImgData.SequenceEqual(imgData) == true);

                    if (sprite is null)
                    {
                        sprite = new Pointer<Starfy4FontSprite>(currentPointer, new Starfy4FontSprite
                        {
                            Byte_00 = (byte)(isSmallFont ? 8 : 13),
                            Byte_01 = (byte)(isSmallFont ? 2 : 4),
                            ImgData = imgData,
                        });

                        sprite.Value.Init(currentPointer);
                        sprite.Value.RecalculateSize();
                        currentPointer += sprite.Value.Size;
                    }

                    fontSprites.Add(sprite);
                }
            }

            font.FontSprites = fontSprites.ToArray();

            FileFactory.Write<Starfy4Font>(context, outputFilePath);

            DisplaySuccessMessage("Font successfully imported");
        }
        catch (Exception ex)
        {
            ShowException(ex);
        }
    }

    #endregion
}