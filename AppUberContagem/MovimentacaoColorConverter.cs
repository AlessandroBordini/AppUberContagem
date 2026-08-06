using System.Globalization;

namespace AppUberContagem
{
    public class MovimentacaoColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string tipo)
            {
                if (tipo == "Ganho")
                    return Colors.Green;
                else if (tipo == "Gasto")
                    return Colors.Red;
            }
            return Colors.Black;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}