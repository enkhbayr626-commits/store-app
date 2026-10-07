using Microsoft.Extensions.Logging;

using Microsoft.Maui.Handlers;

namespace store_app
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    EntryHandler.Mapper.AppendToMapping("NoUnderline", (handler, view) =>
                    {
                        handler.PlatformView.Background = null;
                        handler.PlatformView.SetPadding(0, 0, 0, 0);
                    });
#endif
                })
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
