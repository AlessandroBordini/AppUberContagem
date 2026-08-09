using System.Text;

namespace AppUberContagem.Helpers;

/// <summary>
/// Permite digitar apenas números reais (dígitos e no máximo um separador decimal: vírgula ou ponto).
/// </summary>
public class EntradaNumeroRealBehavior : Behavior<Entry>
{
    protected override void OnAttachedTo(Entry bindable)
    {
        bindable.TextChanged += OnTextChanged;
        base.OnAttachedTo(bindable);
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        bindable.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(bindable);
    }

    private static void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is not Entry entry)
            return;

        string filtrado = Filtrar(e.NewTextValue);
        if (entry.Text != filtrado)
            entry.Text = filtrado;
    }

    public static string Filtrar(string? texto)
    {
        if (string.IsNullOrEmpty(texto))
            return string.Empty;

        var sb = new StringBuilder(texto.Length);
        bool temSeparador = false;

        foreach (char c in texto)
        {
            if (char.IsDigit(c))
            {
                sb.Append(c);
            }
            else if ((c == ',' || c == '.') && !temSeparador)
            {
                sb.Append(c);
                temSeparador = true;
            }
        }

        return sb.ToString();
    }
}
