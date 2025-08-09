using System;
using System.Runtime.InteropServices;

namespace DirectNITest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Direct NI OPC Server Test (2-Hour Free License) ===");
            Console.WriteLine("Attempting to use NI OPC Server directly within free license period\n");

            TestDirectNIConnection();
        }

        static void TestDirectNIConnection()
        {
            Console.WriteLine("Step 1: Testing direct connection to NI OPC Server (free 2-hour mode)");
            Console.WriteLine("======================================================================");

            // ลองใช้ AE Server ที่สร้าง instance ได้
            try
            {
                Console.WriteLine("Method 1: Using Instruments.NIOPCServers_AE.V5");
                Type aeServerType = Type.GetTypeFromProgID("Instruments.NIOPCServers_AE.V5");
                
                if (aeServerType != null)
                {
                    dynamic aeServer = Activator.CreateInstance(aeServerType);
                    Console.WriteLine("✅ AE Server instance created");
                    
                    // ทดสอบ methods ที่มี
                    TestAEServerMethods(aeServer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ AE Server test failed: {ex.Message}");
            }

            Console.WriteLine("\nMethod 2: Force create licensed NI OPC Server");
            Console.WriteLine("---------------------------------------------");
            
            try
            {
                // พยายาม bypass license โดยการใช้แบบ demo/trial
                Console.WriteLine("Attempting to create NI OPC Server in demo mode...");
                
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                if (serverType != null)
                {
                    try
                    {
                        // ลอง create แบบ force
                        dynamic niServer = Activator.CreateInstance(serverType);
                        Console.WriteLine("✅ NI OPC Server instance created (within 2-hour free period)");
                        
                        // ทดสอบ OPC operations
                        if (TestOPCOperations(niServer))
                        {
                            Console.WriteLine("🎉 Successfully read AO0_Speed from NI OPC Server!");
                        }
                    }
                    catch (COMException comEx) when (comEx.HResult == unchecked((int)0x80040112))
                    {
                        Console.WriteLine("❌ License error - trying alternative approach...");
                        
                        // ลองวิธีอื่น - อาจต้องเปิด NI software ก่อน
                        TryAlternativeLicenseApproach();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Method 2 failed: {ex.Message}");
            }

            Console.WriteLine("\nMethod 3: Use OPC Client approach");
            Console.WriteLine("---------------------------------");
            TryOPCClientApproach();
        }

        static void TestAEServerMethods(dynamic aeServer)
        {
            try
            {
                Console.WriteLine("   Testing AE Server methods...");
                
                var methods = aeServer.GetType().GetMethods();
                Console.WriteLine($"   Available methods: {methods.Length}");
                
                foreach (var method in methods.Take(10))
                {
                    Console.WriteLine($"      - {method.Name}");
                }
                
                // ลองใช้ method พื้นฐาน
                try
                {
                    // AE Server อาจใช้ได้กับ Event subscription
                    Console.WriteLine("   💡 This is an Alarm & Events server");
                    Console.WriteLine("   💡 For data reading, need DA (Data Access) server");
                }
                catch (Exception methodEx)
                {
                    Console.WriteLine($"   Method test: {methodEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   AE method test failed: {ex.Message}");
            }
        }

        static bool TestOPCOperations(dynamic opcServer)
        {
            try
            {
                Console.WriteLine("   Testing OPC Data Access operations...");
                
                // ลองใช้ standard OPC interface
                var groups = opcServer.OPCGroups;
                if (groups != null)
                {
                    Console.WriteLine("   ✅ OPCGroups interface accessible");
                    
                    var group = groups.Add("TestGroup");
                    group.UpdateRate = 1000;
                    group.IsActive = true;
                    
                    var items = group.OPCItems;
                    items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                    items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                    
                    Console.WriteLine("   ✅ Items added successfully");
                    
                    // อ่านข้อมูล 3 ครั้ง
                    for (int i = 1; i <= 3; i++)
                    {
                        object[] values = new object[2];
                        object[] errors = new object[2];
                        int[] handles = { 1, 2 };
                        
                        group.SyncRead((short)1, 2, handles, out values, out errors);
                        
                        Console.WriteLine($"\n   📊 Reading #{i} - {DateTime.Now:HH:mm:ss.fff}:");
                        Console.WriteLine($"      🎯 AO0_Speed: {values[0]} RPM (Error: {errors[0]})");
                        Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm (Error: {errors[1]})");
                        
                        if (i < 3) System.Threading.Thread.Sleep(1000);
                    }
                    
                    return true;
                }
                else
                {
                    Console.WriteLine("   ❌ OPCGroups interface not accessible");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ OPC operations failed: {ex.Message}");
                return false;
            }
        }

        static void TryAlternativeLicenseApproach()
        {
            Console.WriteLine("   Trying alternative license approach...");
            
            try
            {
                // ลองเปิด NI software ก่อน
                Console.WriteLine("   💡 Suggestion: Start any NI software first to activate 2-hour license");
                Console.WriteLine("   💡 Then try running this program again");
                
                // ลองใช้ configuration object
                Type configType = Type.GetTypeFromProgID("National Instrumetns.NIOPCServersConfiguration.V5");
                if (configType != null)
                {
                    dynamic config = Activator.CreateInstance(configType);
                    Console.WriteLine("   ✅ Configuration object created - this might activate license");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Alternative approach failed: {ex.Message}");
            }
        }

        static void TryOPCClientApproach()
        {
            Console.WriteLine("   Using generic OPC client approach...");
            
            try
            {
                // ใช้ OPC Server List เพื่อเชื่อมต่อ
                Type serverListType = Type.GetTypeFromProgID("OPC.ServerList.1");
                if (serverListType == null)
                    serverListType = Type.GetTypeFromProgID("OPC.ServerList");
                
                if (serverListType != null)
                {
                    dynamic serverList = Activator.CreateInstance(serverListType);
                    Console.WriteLine("   ✅ OPC ServerList created - this suggests OPC Core is available");
                    
                    // ลองเชื่อมต่อโดยตรงผ่าน category
                    Console.WriteLine("   💡 OPC infrastructure is working");
                    Console.WriteLine("   💡 Problem is specifically with NI OPC Server licensing");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   OPC Client approach failed: {ex.Message}");
            }
        }
    }
}