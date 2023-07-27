# KGuiV2 (WIP)

Modified gui for Karhu RAM Test originally created by RaINi_.

![preview](/.github/resources/preview2-220.png)

# Change Notes - V2.2.0 (JJFX):
- Displays maximum recorded bandwidth (hover mouse for realtime reading)
- Input validation
- Fixed sizing and type issues
- Removed history tab until finished

# Requirements:
* Karhu RAM Test
* .NET 6 or higher

# Installation
* Download and install .NET 6 SDK (if needed)
* Build project in cmd prompt from master directory: dotnet build KGuiV2.csproj
* KGuiV2.exe will be compiled in the 'bin' directory under 'Debug'
* Put 'vcomp140.dll' and 'ramtest.dll' from your Karhu's './x64/' directory into the new KGuiV2.exe directory
* Run the program

# TODO:
- Fix bug with start button not activating the first time
- Options to auto-calculate test size and threads
- Finish history feature