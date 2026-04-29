using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using Microsoft.Extensions.Logging;
using Syncfusion.Maui.Toolkit.Hosting;
using XFrame.Core.Interfaces;
using XFrame.Core.Services;

namespace XFrame;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            // Only use the toolkit on actual platforms, not the "plain library" target
#if !NET10_0 || ANDROID || IOS || WINDOWS || MACCATALYST
            .UseMauiCommunityToolkit()
#endif
            .ConfigureSyncfusionToolkit()
            .ConfigureMauiHandlers(handlers =>
            {
#if WINDOWS
                Microsoft.Maui.Controls.Handlers.Items.CollectionViewHandler.Mapper.AppendToMapping("KeyboardAccessibleCollectionView", (handler, view) =>
                {
                    handler.PlatformView.SingleSelectionFollowsFocus = false;
                });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", FluentUI.FontFamily);
            });

#if DEBUG
        builder.Logging.AddDebug();
        builder.Services.AddLogging(configure => configure.AddDebug());
#endif
        builder.Services.AddSingleton<IXmlProcessorService, XmlProcessorService>();
        builder.Services.AddSingleton<IFileService, FileService>();
        builder.Services.AddSingleton(FileSaver.Default);
        builder.Services.AddSingleton<INotificationService, UiNotificationService>();
        builder.Services.AddSingleton<MainPageModel>();

        return builder.Build();
    }
}