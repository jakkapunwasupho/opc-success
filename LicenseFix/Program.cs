using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace LicenseFix
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== NI OPC License Activation Test ===");
            Console.WriteLine("Trying different methods to activate 2-hour license\n");

            TryActivateLicense();
        }

        static void TryActivateLicense()
        {
            Console.WriteLine("Method 1: Direct license activation via NI services");
            Console.WriteLine("==================================================");
            
            try
            {
                // Method 1: เรียกใช้ NI License Manager
                TryNILicenseManager();
                
                // Method 2: เรียก NI OPC Configuration
                TryNIOPCConfiguration();
                
                // Method 3: เรียก server runtime ให้ทำงานก่อน
                TryServerRuntime();
                
                // Method 4: ทดสอบ license หลังจาก activate
                TestLicenseAfterActivation();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"License activation failed: {ex.Message}");
            }
        }

        static void TryNILicenseManager()
        {
            Console.WriteLine("\nStarting NI License Manager...");
            
            try
            {
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "C:\\Program Files (x86)\\National Instruments\\Shared\\License Manager\\bin\\lmgrd.exe",
                    Arguments = "-c \"C:\\Program Files (x86)\\National Instruments\\Shared\\License Manager\\licenses\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    Console.WriteLine("✅ License Manager started");
                    Thread.Sleep(2000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ License Manager failed: {ex.Message}");
            }
        }

        static void TryNIOPCConfiguration()
        {
            Console.WriteLine("\nTrying NI OPC Configuration to activate license...");
            
            try
            {
                Type configType = Type.GetTypeFromProgID("National Instrumetns.NIOPCServersConfiguration.V5");
                if (configType != null)
                {
                    dynamic config = Activator.CreateInstance(configType);
                    Console.WriteLine("✅ Configuration object created");
                    
                    // ลองเรียก methods ที่อาจ activate license
                    try
                    {
                        var methods = config.GetType().GetMethods();
                        Console.WriteLine($"Available methods: {methods.Length}");
                        
                        foreach (var method in methods)
                        {
                            if (method.Name.Contains("Initialize") || method.Name.Contains("Start") || method.Name.Contains("Activate"))
                            {
                                Console.WriteLine($"Found potential activation method: {method.Name}");
                                try
                                {
                                    if (method.GetParameters().Length == 0)
                                    {
                                        method.Invoke(config, null);
                                        Console.WriteLine($"✅ Called {method.Name}");
                                    }
                                }
                                catch (Exception methodEx)
                                {
                                    Console.WriteLine($"❌ {method.Name} failed: {methodEx.Message}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Method exploration failed: {ex.Message}");
                    }
                    
                    Thread.Sleep(3000); // รอให้ license activate
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ OPC Configuration failed: {ex.Message}");
            }
        }

        static void TryServerRuntime()
        {
            Console.WriteLine("\nEnsuring server runtime is active...");
            
            try
            {
                // เรียก server runtime แบบ background
                var processInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "C:\\Program Files (x86)\\National Instruments\\Shared\\NI OPC Servers\\V5\\server_runtime.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                
                var process = System.Diagnostics.Process.Start(processInfo);
                if (process != null)
                {
                    Console.WriteLine("✅ Server runtime started");
                    Thread.Sleep(3000); // รอให้ server พร้อม
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Server runtime failed: {ex.Message}");
            }
        }

        static void TestLicenseAfterActivation()
        {
            Console.WriteLine("\nTesting license after activation attempts...");
            Console.WriteLine("=============================================");
            
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                Console.WriteLine($"\nAttempt #{attempt}:");
                
                try
                {
                    Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                    if (serverType == null)
                    {
                        Console.WriteLine("❌ Server type not found");
                        continue;
                    }

                    dynamic opcServer = Activator.CreateInstance(serverType);
                    Console.WriteLine("🎉 SUCCESS! OPC Server instance created!");
                    
                    // ถ้าสร้าง instance ได้ ลองอ่านข้อมูล
                    if (TryReadRealData(opcServer))
                    {
                        Console.WriteLine("🎯 Successfully read REAL AO0_Speed data!");
                        return;
                    }
                }
                catch (COMException comEx)
                {
                    Console.WriteLine($"❌ Attempt #{attempt} - COM Error: 0x{comEx.HResult:X8}");
                    
                    if (comEx.HResult == unchecked((int)0x80040112))
                    {
                        Console.WriteLine("   Still license error - waiting longer...");
                        if (attempt < 3)
                        {
                            Thread.Sleep(5000); // รอ 5 วินาที
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   Different error: {comEx.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Attempt #{attempt} failed: {ex.Message}");
                }
            }
            
            Console.WriteLine("\n❌ All license activation attempts failed");
            Console.WriteLine("💡 May need to manually start NI software first");
            Console.WriteLine("💡 Or install proper NI OPC Server license");
        }

        static bool TryReadRealData(dynamic opcServer)
        {
            try
            {
                Console.WriteLine("   Attempting to read real ADAM-5024 data...");
                
                var groups = opcServer.OPCGroups;
                var group = groups.Add("RealDataGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;
                group.IsSubscribed = true;

                var items = group.OPCItems;
                items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);

                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };

                group.SyncRead((short)1, 2, handles, out values, out errors);

                Console.WriteLine("   📊 REAL OPC Data:");
                Console.WriteLine($"      🎯 AO0_Speed: {values[0]} RPM (Error: {errors[0]})");
                Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm (Error: {errors[1]})");

                opcServer.Disconnect();
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Data read failed: {ex.Message}");
                return false;
            }
        }
    }
}