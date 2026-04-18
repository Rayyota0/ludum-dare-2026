NuGet package Vosk was used to vendor Vosk.dll and native libvosk builds (Linux x86_64, Windows x64, macOS universal).

Native libs are configured for Standalone builds only (Editor disabled) so the Linux Editor does not dlopen libvosk at startup — some setups crashed when the editor preloaded Vosk.

For speech recognition inside the Editor on Linux, select libvosk.so → Inspector → enable “Editor” and “Linux64”, apply, restart the editor once.

Managed Vosk.dll stays enabled for the Editor so the project compiles; at runtime it loads libvosk only when you create a Model (standalone player loads plugins from the build).
