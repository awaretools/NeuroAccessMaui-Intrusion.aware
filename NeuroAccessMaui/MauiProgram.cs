﻿using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Markup;
using Microsoft.Extensions.Logging;
using NeuroAccessMaui.Pages;
using NeuroAccessMaui.Resources.Languages;
using NeuroAccessMaui.Services.Localization;

namespace NeuroAccessMaui;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		MauiAppBuilder Builder = MauiApp.CreateBuilder();

		Builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseMauiCommunityToolkitMarkup()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		Builder.UseLocalizationManager<AppResources>();
		Builder.RegisterPagesManager();

		//Builder.Services.AddLogging();
#if DEBUG
		Builder.Logging.AddDebug();
#endif

		return Builder.Build();
	}
}
