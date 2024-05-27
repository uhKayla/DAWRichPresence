# DAW Rich Presence

The idea of this plugin is to provide Discord rich presence functionality in the form of a VST3 plugin.

### How it works
Two components are used, the primary plugin itself, and a service executable. These two components talk over a pipe to transmit data about the current session, such as the state of the DAW, and other quirks. The plugin automatically launches the service executable with a flag stating the type of host we are using, in order to select the correct Client ID with Discord's RPC. 

Note that this plugin is in an unusable development state, I am not responsible for blowing up your projects as this is purely development only. This is a pet-project of mine that will likely never be finished :).

Uses:\
NPlug\
NativeAOT\
.NET\
DiscordRPC for .NET
