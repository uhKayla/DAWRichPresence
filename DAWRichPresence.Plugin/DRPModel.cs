using NPlug;

namespace DAWRichPresence;

public class DRPModel : AudioProcessorModel
{
    public DRPModel() : base("DAWRichPresence")
    {
        AddByPassParameter();
    }
}