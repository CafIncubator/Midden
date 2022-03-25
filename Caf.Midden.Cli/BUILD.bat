dotnet publish -c Release -o Publish/linux-x64 -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r linux-x64 /p:DebugType=None /p:DebugSymbols=false

dotnet publish -c Release -o Publish/win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r win-x64 /p:DebugType=None /p:DebugSymbols=false

dotnet publish -c Release -o Publish/osx.10.11-x64 -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r osx.10.13-x64 /p:DebugType=None /p:DebugSymbols=false
