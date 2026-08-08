using PdfSharpCore.Fonts;

namespace AppUberContagem;

public class SimpleFontResolver : IFontResolver
{
    private readonly byte[] _fontData;

    // Propriedade obrigatória exigida pela interface no PdfSharpCore
    public string DefaultFontName => "OpenSans";

    public SimpleFontResolver(byte[] fontData)
    {
        _fontData = fontData;
    }

    public byte[] GetFont(string faceName)
    {
        // Retorna sempre a fonte que carregamos na memória
        return _fontData;
    }

    public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        // Ignora qualquer nome de fonte (Arial, etc) e força usar a OpenSans
        return new FontResolverInfo("OpenSans");
    }
}