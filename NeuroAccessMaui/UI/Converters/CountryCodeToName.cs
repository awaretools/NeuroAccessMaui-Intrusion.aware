﻿using System.Globalization;
using NeuroAccessMaui.Services.Data;

namespace NeuroAccessMaui.UI.Converters
{
	/// <summary>
	/// Converts a country code to a country name.
	/// </summary>
	public class CountryCodeToName : IValueConverter, IMarkupExtension
	{
		/// <inheritdoc/>
		public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			if (value is string Code && ISO_3166_1.TryGetCountryByCode(Code, out ISO3166Country? Country) && Country is not null)
				return Country.Name;
			else
				return value ?? string.Empty;
		}

		/// <inheritdoc/>
		public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		{
			return value;
		}

		/// <inheritdoc/>
		public object ProvideValue(System.IServiceProvider serviceProvider)
		{
			return this;
		}
	}
}
