using System.Runtime.CompilerServices;
using NPlug;

namespace DAWRichPresence;

public class DRPPlugin
{
    public static AudioPluginFactory GetFactory()
    {
        var factory = new AudioPluginFactory(new("DAWRichPresence", "https://github.com/uhKayla/DAWRichPresence", "archangel@angelware.net"));
        factory.RegisterPlugin<DRPProcessor>(new(DRPProcessor.ClassId, "DAW Rich Presence", AudioProcessorCategory.Effect));
        factory.RegisterPlugin<DRPController>(new(DRPController.ClassId, "DAW Rich Presence Controller"));
        return factory;
    }

    [ModuleInitializer]
    internal static void ExportThisPlugin()
    {
        AudioPluginFactoryExporter.Instance = GetFactory();
    }
}