@echo off
setlocal

dotnet publish -c Release -f net10.0 -o Publish/linux-x64 -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r linux-x64 /p:DebugType=None /p:DebugSymbols=false
dotnet publish -c Release -f net10.0 -o Publish/win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r win-x64 /p:DebugType=None /p:DebugSymbols=false
dotnet publish -c Release -f net10.0 -o Publish/osx-x64 -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r osx-x64 /p:DebugType=None /p:DebugSymbols=false
dotnet publish -c Release -f net10.0 -o Publish/osx-arm64 -p:PublishReadyToRun=false -p:PublishSingleFile=true -p:UseAppHost=true --self-contained true -p:IncludeNativeLibrariesForSelfExtract=true -r osx-arm64 /p:DebugType=None /p:DebugSymbols=false
