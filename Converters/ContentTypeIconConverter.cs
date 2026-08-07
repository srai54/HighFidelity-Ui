using System.Globalization;

namespace HighFidelity.Ui.Converters;

/// <summary>
/// Maps a document's ContentType (MIME type) to a FontAwesome glyph for the
/// file-type icon shown next to each row in the Documents list.
/// </summary>
public class ContentTypeIconConverter : IValueConverter
{
    private const string FilePdf = "\uf1c1";
    private const string FileImage = "\uf1c5";
    private const string FileWord = "\uf1c2";
    private const string FileExcel = "\uf1c3";
    private const string FileText = "\uf0f6";
    private const string FileGeneric = "\uf016";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var contentType = value?.ToString() ?? string.Empty;

        if (contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase)) return FilePdf;
        if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)) return FileImage;
        if (contentType.Contains("word", StringComparison.OrdinalIgnoreCase) || contentType.Contains("msword", StringComparison.OrdinalIgnoreCase)) return FileWord;
        if (contentType.Contains("sheet", StringComparison.OrdinalIgnoreCase) || contentType.Contains("excel", StringComparison.OrdinalIgnoreCase)) return FileExcel;
        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)) return FileText;

        return FileGeneric;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
