# KGuiV2 (WIP)

Modified gui for Karhu RAM Test originally created by RaINi_.

![preview](/.github/resources/preview-212.png)

# Change Notes - V2.1.2 (JJFX):
- Custom app icon
- Added minimize button
- Fixed display of system memory
- Fixed previous test size not saving
- Fixed speed value and infinity error
- Speed value locked when test is not active
- Crude fix for start button not enabling at launch
- Value formatting based on system region
- Modified setting labels and formatting

# TODO
- Calculate appropriate test size automatically
- Add history log

# Requirements
* .NET 6

# Installation
* Download and install .NET 6 SDK (if needed)
* Build project in cmd prompt from master directory: dotnet build KGuiV2.csproj
* KGuiV2.exe will be compiled in the 'bin' directory under 'Debug'
* Put 'vcomp140.dll' and 'ramtest.dll' from your Karhu's './x64/' directory into the new KGuiV2.exe directory
* Run the program