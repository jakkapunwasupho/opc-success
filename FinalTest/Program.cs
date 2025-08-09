using System;
using System.Runtime.InteropServices;

namespace FinalOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Final OPC AO0_Speed Test ===");
            Console.WriteLine("Testing all available methods to read AO0_Speed\n");

            TestAllOPCMethods();
        }

        static void TestAllOPCMethods()
        {
            Console.WriteLine("Comprehensive OPC Server Testing");
            Console.WriteLine("=================================");

            // Method 1: Try original approach with fresh license check
            Console.WriteLine("\nMethod 1: Direct NI OPC Server Connection");
            Console.WriteLine("------------------------------------------");
            TryDirectNIConnection();

            // Method 2: Try with Configuration first
            Console.WriteLine("\nMethod 2: Via NI OPC Configuration");
            Console.WriteLine("-----------------------------------");
            TryViaConfiguration();

            // Method 3: Try OPC Automation if available
            Console.WriteLine("\nMethod 3: OPC Automation Approach");
            Console.WriteLine("----------------------------------");
            TryOPCAutomation();

            // Method 4: Try alternative server approaches
            Console.WriteLine("\nMethod 4: Alternative Server Names");
            Console.WriteLine("-----------------------------------");
            TryAlternativeServers();
        }

        static void TryDirectNIConnection()
        {
            try
            {
                Console.WriteLine("Creating NI OPC Server instance...");
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                
                if (serverType == null)
                {
                    Console.WriteLine("❌ Server type not found");
                    return;
                }

                dynamic opcServer = Activator.CreateInstance(serverType);
                Console.WriteLine("✅ Instance created successfully!");

                // ทดสอบการใช้งาน OPC
                if (TestOPCDataAccess(opcServer, "National Instruments.NIOPCServers.V5"))
                {
                    Console.WriteLine("🎉 SUCCESS: Read AO0_Speed via direct NI connection!");
                    return;
                }
            }
            catch (COMException comEx)
            {
                Console.WriteLine($"❌ COM Error: {comEx.Message}");
                Console.WriteLine($"   HRESULT: 0x{comEx.HResult:X8}");
                
                if (comEx.HResult == unchecked((int)0x80040112))
                {
                    Console.WriteLine("   🔍 License not activated yet - may need more time");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void TryViaConfiguration()
        {
            try
            {
                Console.WriteLine("Using NI OPC Configuration object...");
                Type configType = Type.GetTypeFromProgID("National Instrumetns.NIOPCServersConfiguration.V5");
                
                if (configType != null)
                {
                    dynamic config = Activator.CreateInstance(configType);
                    Console.WriteLine("✅ Configuration object created");
                    
                    // Configuration object อาจช่วย initialize license
                    try
                    {
                        var methods = config.GetType().GetMethods();
                        Console.WriteLine($"   Configuration methods available: {methods.Length}");
                        
                        // ลองใช้ configuration เพื่อ activate server
                        Console.WriteLine("   💡 Configuration created - this may help activate license");
                        
                        // หลังจากใช้ config แล้วลอง create server อีกครั้ง
                        System.Threading.Thread.Sleep(2000);
                        TryDirectNIConnection();
                    }
                    catch (Exception configEx)
                    {
                        Console.WriteLine($"   Configuration usage: {configEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Configuration method failed: {ex.Message}");
            }
        }

        static void TryOPCAutomation()
        {
            string[] automationProgIds = {
                "OPC.Automation.1",
                "OPC.Automation",
                "Matrikon.OPC.Automation.1",
                "OPCEXPERT.OPCExpertApplication.1"
            };

            foreach (string progId in automationProgIds)
            {
                try
                {
                    Console.WriteLine($"Testing: {progId}");
                    Type automationType = Type.GetTypeFromProgID(progId);
                    
                    if (automationType != null)
                    {
                        dynamic automation = Activator.CreateInstance(automationType);
                        Console.WriteLine($"✅ {progId} created");
                        
                        try
                        {
                            automation.Connect("National Instruments.NIOPCServers.V5");
                            Console.WriteLine($"🎯 Connected via {progId}!");
                            
                            if (TestOPCDataAccess(automation, progId))
                            {
                                Console.WriteLine($"🎉 SUCCESS: Read AO0_Speed via {progId}!");
                                automation.Disconnect();
                                return;
                            }
                            
                            automation.Disconnect();
                        }
                        catch (Exception connectEx)
                        {
                            Console.WriteLine($"   Connection failed: {connectEx.Message}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ {progId} not found");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ {progId} test failed: {ex.Message}");
                }
            }
        }

        static void TryAlternativeServers()
        {
            string[] alternativeServers = {
                "Matrikon.OPC.Server",
                "KEPware.KEPServerEx.V5",
                "OPC.Server.1",
                "Generic.OPC.Server"
            };

            foreach (string serverName in alternativeServers)
            {
                try
                {
                    Console.WriteLine($"Testing alternative server: {serverName}");
                    Type serverType = Type.GetTypeFromProgID(serverName);
                    
                    if (serverType != null)
                    {
                        dynamic server = Activator.CreateInstance(serverType);
                        Console.WriteLine($"✅ {serverName} found and created");
                        
                        if (TestOPCDataAccess(server, serverName))
                        {
                            Console.WriteLine($"🎉 SUCCESS: Read AO0_Speed via {serverName}!");
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ {serverName} not available");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ {serverName} test failed: {ex.Message}");
                }
            }
        }

        static bool TestOPCDataAccess(dynamic opcServer, string serverName)
        {
            try
            {
                Console.WriteLine($"   Testing OPC data access via {serverName}...");
                
                var groups = opcServer.OPCGroups;
                if (groups == null)
                {
                    Console.WriteLine($"   ❌ OPCGroups not accessible");
                    return false;
                }

                var group = groups.Add("TestGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;
                group.IsSubscribed = true;

                var items = group.OPCItems;
                items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);

                Console.WriteLine($"   ✅ Items added successfully");

                // อ่านข้อมูลจริง
                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };

                group.SyncRead((short)1, 2, handles, out values, out errors);

                Console.WriteLine($"\n   📊 REAL OPC DATA from {serverName}:");
                Console.WriteLine($"   ╔══════════════════════════════════════╗");
                Console.WriteLine($"   ║  🎯 AO0_Speed:  {values[0],-12} RPM      ║");
                Console.WriteLine($"   ║  ⚡ AO1_Torque: {values[1],-12} Nm       ║");
                Console.WriteLine($"   ║  ❗ Errors:     [{errors[0]}, {errors[1]}]              ║");
                Console.WriteLine($"   ║  ⏰ Time:       {DateTime.Now:HH:mm:ss.fff}         ║");
                Console.WriteLine($"   ╚══════════════════════════════════════╝");

                // อ่านอีก 2 ครั้งเพื่อดูการเปลี่ยนแปลง
                for (int i = 2; i <= 3; i++)
                {
                    System.Threading.Thread.Sleep(1000);
                    group.SyncRead((short)1, 2, handles, out values, out errors);
                    
                    Console.WriteLine($"\n   📊 Reading #{i} - {DateTime.Now:HH:mm:ss.fff}:");
                    Console.WriteLine($"      🎯 AO0_Speed: {values[0]} RPM");
                    Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm");
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Data access test failed: {ex.Message}");
                return false;
            }
        }
    }
}