msbuild ../chatinput/chatinput.csproj /t:Clean /t:Rebuild /verbosity:quiet /p:Configuration=Release;DebugType=none;TargetFrameworkVersion=v8.1
msbuild ../messagelist/messagelist.csproj /t:Clean /t:Rebuild /verbosity:quiet /p:Configuration=Release;DebugType=none;TargetFrameworkVersion=v8.1

nuget pack IMUI.Android.ChatInput.nuspec -Version 0.10.0
nuget pack IMUI.Android.Messagelist.nuspec -Version 0.8.0