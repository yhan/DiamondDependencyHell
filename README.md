
# DLL Identity

dotnet dll's identity is defined by its strong name (name, version, culture, public key token).
When an assembly references another assembly, it records the exact identity of the referenced assembly.
At runtime, the .NET Framework loader uses this identity to locate and load the referenced assembly.
If the exact version of the assembly is not found, a FileNotFoundException is thrown.


# Nuget package downgrade warning
````
Warning As Error: Detected package downgrade: log4net from 3.2.0 to 1.2.13. Reference the package directly from the
project to select a different version.
TestMultiLog4net -> ATSLib 1.0.0 -> log4net (>= 3.2.0)
TestMultiLog4net -> log4net (>= 1.2.13)

you can make ATSLib nuget targetting log4net (>=1.2.13 )
````

dotnet build -c Release  
dotnet pack -c Release -p:NuspecFile=ATSLib.nuspec (or nuget pack ATSLib.nuspec)  

Top level project TestMultiLog4net references ATSLib 1.1.0 and log4net 1.2.13
ATSLib 1.0.0 references log4net = 3.2.0
ATSLib nupkg should reference log4net (>=1.2.13) to avoid downgrade warning.
Add ATSLib 1.1.0 to TestMultiLog4net project OK.

But at top level app TestMultiLog4net, runtime throws:

````
System.IO.FileNotFoundException: Could not load file or assembly 'log4net, Version=3.2.0.0, Culture=neutral,
PublicKeyToken=669e0ddf0bb1aa2a'. The system cannot find the file specified.
File name: 'log4net, Version=3.2.0.0, Culture=neutral, PublicKeyToken=669e0ddf0bb1aa2a'
at ATSLib.Apple.DoSomething()
at Program.Main(String[] args) in C:\Users\hanyi\RiderProjects\TestMultiLog4net\TestMultiLog4net\Program.cs:line 24
````

TestMultiLog4net.deps.json says everything is on log4net 1.2.13 (including ATSLib 1.1.0). So the host plans to load 1.2.13 — confirmed in
your TestMultiLog4net.deps.json.

But the crash proves ATSLib.dll itself was compiled against log4net, Version=3.2.0.0 (your ILSpy screenshot shows that).
On .NET (Core/5+/8) the loader must satisfy the exact strong-name version embedded in the referencing assembly; there’s
no binding redirect/unification. So even if deps.json brings 1.2.13, the type loader still looks for 3.2.0.0 and throws.


# Legacy dotnet Framework

## Public Key Token is part of strong name identity

Binding redirect does not work if publicKeyToken mismatch.

The same assembly name + culture + public key token can makes binding redirect work.

````xml
<?xml version="1.0" encoding="utf-8"?>

<configuration>
    <runtime>
        <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
            <dependentAssembly>
                <assemblyIdentity name="log4net"
                                  publicKeyToken="669e0ddf0bb1aa2a"
                                  culture="neutral" />
                <bindingRedirect oldVersion="0.0.0.0-3.2.0.0"
                                 newVersion="1.2.14.0" />
            </dependentAssembly>
        </assemblyBinding>
    </runtime>
</configuration>
````
My built 1.2.14 with the same public key as apache log4net 3.2.0:
log4net, Version=1.2.14.0, Culture=neutral, PublicKeyToken=669e0ddf0bb1aa2a  (apache log4net public key)
log4net, Version=3.2.0.0, Culture=neutral, PublicKeyToken=669e0ddf0bb1aa2a


# Solution for ATSLib / NOVA

NOVA needs log4net 1.2.13 with a different public key token than the one of official apache log4net.
.NET (Core/5+/8) the loader must satisfy the exact strong-name version of log4net embedded in the ATSLib.dll.
.NET (Core/5+/8) supports no more binding redirect/unification.

ATSLib should:
1. build against log4net 1.2.13 with NOVA public key token

2. nuget package ref log4net

   * Option 1: should reference log4net (>=1.2.13) to avoid downgrade warning.
   ````nuspec
    <?xml version="1.0"?>

    <package>
        <metadata>
            <id>ATSLib</id>
            <version>1.1.0</version>
            <authors>YourName</authors>
            <description>ATSLib library.</description>
            <license type="expression">MIT</license>
            <dependencies>
                <dependency id="log4net" version="[1.2.13,)"/>
            </dependencies>
        </metadata>
        <files>
            <file src="bin\Release\netstandard2.0\ATSLib.dll" target="lib\netstandard2.0\"/>
            <file src="bin\Release\netstandard2.0\ATSLib.pdb" target="lib\netstandard2.0\"/>
        </files>
    </package>
   ```` 
   * Option 2: does not include log4net as dependency.
   


# Sign manually

To manually sign a DLL with a strong name using sn.exe, follow these steps:
Generate a strong name key file if you don't have one:

````
sn.exe -k log4net.snk
````

Rebuild your DLL with the key:
Add the following attribute to your AssemblyInfo.cs:

````
[assembly: AssemblyKeyFile(@"log4net.snk")]
````

Or, use the /keyfile compiler option:

````
csc /keyfile:log4net.snk /target:library yourfile.cs
````
If the DLL is already built and not signed, use ildasm and ilasm:
````
ildasm your.dll /out=your.il
ilasm your.il /dll /key=log4net.snk
````
This will produce a signed DLL.


# MSBuild & csc ...

dotnet build = dotnet msbuild(.dll) myapp.csproj  


## How csc.dll is invoked ? 
dotnet exec path to csc.dll <args>
This becomes invisible when you invoke dotnet build. Because in SDK-style builds, the C# compile is done by the Roslyn Csc MSBuild task and (by default) it talks  
to the compiler server (VBCSCompiler.exe) instead of spawning a visible dotnet csc.dll process,  
so the command line often isn’t echoed.  

But you can see the task CoreCompile target in bin build  log:
<img width="894" height="245" alt="image" src="https://github.com/user-attachments/assets/95f3fc67-5e75-4800-9cf8-d8144dcdf6eb" />


to view structured msbuild log :  
https://msbuildlog.com/  
dotnet build /bl  
msbuild MyApp.csproj /bl  
dotnet msbuild MyApp.csproj /bl:out/buildlog.binlog  



