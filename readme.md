1. Change output type to Windows Application (no console)

Open your .csproj and add:

<PropertyGroup>
  <OutputType>WinExe</OutputType>
</PropertyGroup>


Exe → console window appears.

WinExe → no console window (runs silently).

Then publish again:

dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeAllContentForSelfExtract=true -p:PublishTrimmed=false -o ./publish

dotnet publish -c Release