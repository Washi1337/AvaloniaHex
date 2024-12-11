using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using AvaloniaHex.Document;
using AvaloniaHex.Editing;
using AvaloniaHex.Rendering;

namespace AvaloniaHex.Demo
{
    public partial class MainWindow : Window
    {
        private readonly Random _random = new();
        private readonly RangesHighlighter _changesHighlighter;
        private readonly ZeroesHighlighter _zeroesHighlighter;
        private readonly InvalidRangesHighlighter _invalidRangesHighlighter;
        private string? _currentFilePath;

        public MainWindow()
        {
            InitializeComponent();

            // Create some custom highlighters.
            _zeroesHighlighter = new ZeroesHighlighter
            {
                Foreground = new SolidColorBrush(new Color(255, 75, 75, 75)),
            };

            _changesHighlighter = new RangesHighlighter
            {
                Foreground = Brushes.Red
            };

            _invalidRangesHighlighter = new InvalidRangesHighlighter
            {
                Foreground = new SolidColorBrush(Colors.Gray, 0.5)
            };

            // Enable the changes highlighter.
            MainHexEditor.HexView.LineTransformers.Add(_changesHighlighter);
            MainHexEditor.HexView.LineTransformers.Add(_invalidRangesHighlighter);

            // Divide each 8 bytes with a dashed line and separate colors.
            var layer = MainHexEditor.HexView.Layers.Get<CellGroupsLayer>();
            layer.BytesPerGroup = 8;
            layer.Backgrounds.Add(new SolidColorBrush(Colors.Gray, 0.1D));
            layer.Backgrounds.Add(null);
            layer.Border = new Pen(Brushes.Gray, dashStyle: DashStyle.Dash);

            MainHexEditor.DocumentChanged += MainHexEditorOnDocumentChanged;
            MainHexEditor.Selection.RangeChanged += SelectionOnRangeChanged;
            MainHexEditor.Caret.ModeChanged += CaretOnModeChanged;
        }

        protected override async void OnLoaded(RoutedEventArgs e)
        {
            base.OnLoaded(e);
            await OpenFileAsDynamicBuffer(typeof(MainWindow).Assembly.Location);
        }

        private void NewDynamicOnClick(object? sender, RoutedEventArgs e)
        {
            MainHexEditor.Document = new DynamicBinaryDocument();
            _currentFilePath = null;
            Title = $"**NEW** (Dynamic) - AvaloniaHex.Demo";
        }

        private async Task OpenFileAsFixedBuffer(string filePath)
        {
            try
            {
                var document = new MemoryBinaryDocument(await File.ReadAllBytesAsync(filePath));

                _currentFilePath = filePath;
                MainHexEditor.Document = document;
                StatusLabel.Content = $"Opened file {filePath}.";
                Title = $"{_currentFilePath} (Fixed Buffer) - AvaloniaHex.Demo";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Failed to read file: {ex.Message}";
            }
        }

        private async Task OpenFileAsDynamicBuffer(string filePath)
        {
            try
            {
                var document = new DynamicBinaryDocument(await File.ReadAllBytesAsync(filePath));

                _currentFilePath = filePath;
                MainHexEditor.Document = document;
                StatusLabel.Content = $"Opened file {filePath}.";
                Title = $"{_currentFilePath} (Dynamic Buffer) - AvaloniaHex.Demo";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Failed to read file: {ex.Message}";
            }
        }

        private Task OpenFileAsMmio(string filePath)
        {
            try
            {
                var file = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate);
                var document = new MemoryMappedBinaryDocument(file, false);

                _currentFilePath = filePath;
                MainHexEditor.Document = document;
                StatusLabel.Content = $"Opened file {filePath} via MMIO.";
                Title = $"{_currentFilePath} (MMIO) - AvaloniaHex.Demo";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Failed to read file: {ex.Message}";
            }

            return Task.CompletedTask;
        }

        private async Task SaveFile(string filePath)
        {
            try
            {
                switch (MainHexEditor.Document)
                {
                    case MemoryBinaryDocument document:
                        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            await fs.WriteAsync(document.Memory);
                        break;

                    case DynamicBinaryDocument document:
                        await using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                            await fs.WriteAsync(document.ToArray());
                        break;

                    case MemoryMappedBinaryDocument document:
                        if (_currentFilePath != filePath)
                            throw new ArgumentException("Cannot save MMIO document to another file.");
                        document.Flush();
                        break;

                    default:
                        throw new ArgumentException("Cannot save this type of document!");
                }

                _currentFilePath = filePath;
                Title = $"{_currentFilePath} - AvaloniaHex.Demo";
                _changesHighlighter.Ranges.Clear();
                MainHexEditor.HexView.InvalidateVisualLines();
                StatusLabel.Content = $"Saved file {filePath}.";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = $"Failed to save file: {ex.Message}";
            }
        }

        private void SelectionOnRangeChanged(object? sender, EventArgs e)
        {
            StatusLabel.Content = MainHexEditor.Selection.Range.ToString();
        }

        private void CaretOnModeChanged(object? sender, EventArgs e)
        {
            ModeLabel.Content = MainHexEditor.Caret.Mode.ToString();
        }

        private async void OpenFixedOnClick(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File (Fixed)",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("All files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (files.Count != 0 && files[0].TryGetLocalPath() is { } path)
                await OpenFileAsFixedBuffer(path);
        }

        private async void OpenDynamicOnClick(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File (Dynamic)",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("All files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (files.Count != 0 && files[0].TryGetLocalPath() is { } path)
                await OpenFileAsDynamicBuffer(path);
        }

        private async void OpenMmioOnClick(object? sender, RoutedEventArgs e)
        {
            var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open File (MMIO)",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("All files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (files.Count != 0 && files[0].TryGetLocalPath() is { } path)
                await OpenFileAsMmio(path);
        }

        private async void SaveAsOnClick(object? sender, RoutedEventArgs e)
        {
            var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save File",
                SuggestedFileName = _currentFilePath,
                FileTypeChoices =
                [
                    new FilePickerFileType("All files")
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (file?.TryGetLocalPath() is { } path)
                await SaveFile(path);
        }

        private async void SaveOnClick(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
                SaveAsOnClick(sender, e);
            else
                await SaveFile(_currentFilePath);
        }

        private void UppercaseOnClick(object? sender, RoutedEventArgs e)
        {
            var offsetColumn = MainHexEditor.Columns.Get<OffsetColumn>();
            offsetColumn.IsUppercase = !offsetColumn.IsUppercase;

            var hexColumn = MainHexEditor.Columns.Get<HexColumn>();
            hexColumn.IsUppercase = !hexColumn.IsUppercase;
        }

        private void SystemThemeOnClick(object? sender, RoutedEventArgs e)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Default;
        }

        private void LightThemeOnClick(object? sender, RoutedEventArgs e)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Light;
        }

        private void DarkThemeOnClick(object? sender, RoutedEventArgs e)
        {
            Application.Current!.RequestedThemeVariant = ThemeVariant.Dark;
        }

        private void ToggleColumn<TColumn>()
            where TColumn : Column
        {
            var column = MainHexEditor.Columns.Get<TColumn>();
            column.IsVisible = !column.IsVisible;
        }

        private void OffsetOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<OffsetColumn>();

        private void HexOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<HexColumn>();

        private void BinaryOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<BinaryColumn>();

        private void AsciiOnClick(object? sender, RoutedEventArgs e) => ToggleColumn<AsciiColumn>();

        private async void CopyOnClick(object? sender, RoutedEventArgs e) => await MainHexEditor.Copy();

        private void AdjustOnClick(object? sender, RoutedEventArgs e)
        {
            int? actualCount = null;
            if (int.TryParse(((MenuItem)sender!).CommandParameter?.ToString(), out int count))
                actualCount = count;

            MainHexEditor.HexView.BytesPerLine = actualCount;
        }

        private void FontSizeOnClick(object? sender, RoutedEventArgs e)
        {
            double fontSize = 12;
            if (int.TryParse(((MenuItem)sender!).CommandParameter?.ToString(), out int size))
                fontSize = size;

            MainHexEditor.HexView.FontSize = fontSize;
        }

        private void ColumnPaddingOnClick(object? sender, RoutedEventArgs e)
        {
            double columnPadding = 5D;
            if (int.TryParse(((MenuItem)sender!).CommandParameter?.ToString(), out int padding))
                columnPadding = padding;

            MainHexEditor.ColumnPadding = columnPadding;
        }

        private void ToggleHighlighter(ILineTransformer transformer)
        {
            var transformers = MainHexEditor.HexView.LineTransformers;
            if (transformers.Contains(transformer))
                transformers.Remove(transformer);
            else
                transformers.Add(transformer);
            MainHexEditor.HexView.InvalidateVisualLines();
        }

        private void ZeroesOnClick(object? sender, RoutedEventArgs e) => ToggleHighlighter(_zeroesHighlighter);

        private void ChangesOnClick(object? sender, RoutedEventArgs e) => ToggleHighlighter(_changesHighlighter);

        private void InvalidOnClick(object? sender, RoutedEventArgs e) => ToggleHighlighter(_invalidRangesHighlighter);

        private void SegmentedDocumentOnClick(object? sender, RoutedEventArgs e)
        {
            var segments = new List<SegmentedDocument.Mapping>();

            // Add some random mappings.
            for (int i = 0; i < 10; i++)
            {
                segments.Add(new SegmentedDocument.Mapping(
                    (ulong) (i * 2000),
                    Enumerable.Range(0, 1000).Select(x => (byte) (x & 0xFF)).ToArray()
                ));
            }

            _currentFilePath = null;
            MainHexEditor.Document = new SegmentedDocument(segments);
        }

        private void RealTimeChangingDocumentOnClick(object? sender, RoutedEventArgs e)
        {
            var document = new RealTimeChangingDocument(5 * 1024, TimeSpan.FromMilliseconds(100));

            // Add some random dynamic ranges.
            for (int i = 0; i < 10; i++)
            {
                int start = _random.Next((int) document.Length);
                document.DynamicRanges.Add(new BitRange((ulong) start, (ulong) start + 4));
            }

            _currentFilePath = null;
            MainHexEditor.Document = document;
        }

        private async void AvaloniaHexDemoOnClick(object? sender, RoutedEventArgs e)
        {
            await OpenFileAsDynamicBuffer(typeof(MainWindow).Assembly.Location);
        }

        private void OnFillWithZeroesOnClick(object? sender, RoutedEventArgs e)
        {
            var range = MainHexEditor.Selection.Range;
            MainHexEditor.Document?.WriteBytes(range.Start.ByteIndex, new byte[range.ByteLength]);
        }

        private void MainHexEditorOnDocumentChanged(object? sender, DocumentChangedEventArgs e)
        {
            _changesHighlighter.Ranges.Clear();
            if (e.Old is not null)
                e.Old.Changed -= DocumentOnChanged;
            if (e.New is not null)
                e.New.Changed += DocumentOnChanged;
        }

        private void DocumentOnChanged(object? sender, BinaryDocumentChange change)
        {
            switch (change.Type)
            {
                case BinaryDocumentChangeType.Modify:
                    _changesHighlighter.Ranges.Add(change.AffectedRange);
                    break;

                case BinaryDocumentChangeType.Insert:
                case BinaryDocumentChangeType.Remove:
                    _changesHighlighter.Ranges.Add(change.AffectedRange.ExtendTo(MainHexEditor.Document!.ValidRanges.EnclosingRange.End));
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}