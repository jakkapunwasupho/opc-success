using System;
using System.Runtime.InteropServices;

namespace SimpleComOPC
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Simple COM OPC Test ===");
            Console.WriteLine("Testing direct COM connection to NI OPC Server\n");

            TestSimpleCOMConnection();
        }

        static void TestSimpleCOMConnection()
        {
            Console.WriteLine("Attempting direct COM connection to NI OPC Server...");
            Console.WriteLine("====================================================");

            try
            {
                Console.WriteLine("Step 1: Creating COM instance...");
                
                // ลองสร้าง COM object โดยตรง (ใช้ ProgID ที่พบใน registry)
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                
                if (serverType == null)
                {
                    Console.WriteLine("❌ Cannot find 'National Instruments.NIOPCServers.V5' in registry");
                    Console.WriteLine("   Trying alternative server names...");
                    
                    TryAlternativeServerNames();
                    return;
                }

                Console.WriteLine("✅ Found NI OPC Server in registry");
                
                Console.WriteLine("\nStep 2: Creating server instance...");
                dynamic opcServer = Activator.CreateInstance(serverType);
                Console.WriteLine("✅ COM object created successfully");

                Console.WriteLine("\nStep 3: Testing server methods...");
                
                // ทดสอบ method ต่างๆ ที่อาจมี
                TestServerMethods(opcServer);
                
                Console.WriteLine("\nStep 4: Attempting to create groups and items...");
                bool success = TryReadOPCData(opcServer);
                
                if (success)
                {
                    Console.WriteLine("\n🎉 SUCCESS! Retrieved AO0_Speed data from NI OPC Server");
                }
                else
                {
                    Console.WriteLine("\n❌ Could not read OPC data - server may need additional configuration");
                }
            }
            catch (COMException comEx)
            {
                Console.WriteLine($"\n❌ COM Exception: {comEx.Message}");
                Console.WriteLine($"   HRESULT: 0x{comEx.HResult:X8}");
                
                AnalyzeCOMError(comEx.HResult);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ General Error: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
            }
        }

        static void TryAlternativeServerNames()
        {
            string[] serverNames = {
                "National Instruments.NIOPCServers.V5",
                "Instruments.NIOPCServers_AE.V5",
                "National Instrumetns.NIOPCServersConfiguration.V5",
                "NationalInstruments.NIOPCServer",
                "NI.OPCServer.V5",
                "NI.OPCServer"
            };

            Console.WriteLine("Searching for alternative NI OPC Server names:");
            
            foreach (string serverName in serverNames)
            {
                try
                {
                    Console.WriteLine($"   Testing: {serverName}");
                    Type serverType = Type.GetTypeFromProgID(serverName);
                    
                    if (serverType != null)
                    {
                        dynamic opcServer = Activator.CreateInstance(serverType);
                        Console.WriteLine($"   ✅ Found and created: {serverName}");
                        
                        TryReadOPCData(opcServer);
                        return;
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Not found: {serverName}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ Error with {serverName}: {ex.Message}");
                }
            }
        }

        static void TestServerMethods(dynamic opcServer)
        {
            string[] methodsToTest = {
                "GetStatus",
                "CreateGroupEnumerator", 
                "GetErrorString",
                "AddGroup",
                "GetGroupByName",
                "RemoveGroup",
                "CreateGroup"
            };

            foreach (string method in methodsToTest)
            {
                try
                {
                    var methodInfo = opcServer.GetType().GetMethod(method);
                    if (methodInfo != null)
                    {
                        Console.WriteLine($"   ✅ Method available: {method}");
                    }
                    else
                    {
                        Console.WriteLine($"   ❌ Method not found: {method}");
                    }
                }
                catch
                {
                    Console.WriteLine($"   ❌ Error testing method: {method}");
                }
            }

            // ทดสอบ properties
            try
            {
                var properties = opcServer.GetType().GetProperties();
                Console.WriteLine($"   📋 Available properties: {properties.Length}");
                
                foreach (var prop in properties)
                {
                    Console.WriteLine($"      - {prop.Name}: {prop.PropertyType.Name}");
                    
                    if (prop.Name == "OPCGroups")
                    {
                        Console.WriteLine("      ⭐ Found OPCGroups property!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Error getting properties: {ex.Message}");
            }
        }

        static bool TryReadOPCData(dynamic opcServer)
        {
            try
            {
                Console.WriteLine("   → Testing OPC data reading...");
                
                // Method 1: ลองใช้ OPCGroups property
                try
                {
                    var groups = opcServer.OPCGroups;
                    if (groups != null)
                    {
                        Console.WriteLine("   ✅ OPCGroups property accessible");
                        
                        var group = groups.Add("TestGroup");
                        group.UpdateRate = 1000;
                        group.IsActive = true;
                        
                        var items = group.OPCItems;
                        items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                        items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                        
                        Console.WriteLine("   ✅ Items added successfully");
                        
                        // อ่านข้อมูล
                        object[] values = new object[2];
                        object[] errors = new object[2]; 
                        int[] handles = { 1, 2 };
                        
                        group.SyncRead((short)1, 2, handles, out values, out errors);
                        
                        Console.WriteLine("\n   📊 OPC Data Reading Results:");
                        Console.WriteLine($"      🎯 AO0_Speed: {values[0]} RPM");
                        Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm");
                        Console.WriteLine($"      ❗ Error Codes: [{errors[0]}, {errors[1]}]");
                        
                        // ทำซ้ำอีก 2 ครั้ง
                        for (int i = 2; i <= 3; i++)
                        {
                            System.Threading.Thread.Sleep(1000);
                            group.SyncRead((short)1, 2, handles, out values, out errors);
                            
                            Console.WriteLine($"\n   📊 Reading #{i} - {DateTime.Now:HH:mm:ss.fff}:");
                            Console.WriteLine($"      🎯 AO0_Speed: {values[0]} RPM");
                            Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm");
                        }
                        
                        opcServer.Disconnect();
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   ❌ OPCGroups method failed: {ex.Message}");
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Data reading failed: {ex.Message}");
                return false;
            }
        }

        static void AnalyzeCOMError(int hresult)
        {
            switch (hresult)
            {
                case unchecked((int)0x80040154):
                    Console.WriteLine("   🔍 Class not registered");
                    Console.WriteLine("   💡 NI OPC Server may not be properly installed or registered");
                    break;
                    
                case unchecked((int)0x800401F0):
                    Console.WriteLine("   🔍 CoInitialize has not been called");
                    Console.WriteLine("   💡 This is usually handled automatically in .NET");
                    break;
                    
                case unchecked((int)0x80070005):
                    Console.WriteLine("   🔍 Access denied");
                    Console.WriteLine("   💡 Try running as Administrator");
                    break;
                    
                default:
                    Console.WriteLine($"   🔍 Unknown COM error: 0x{hresult:X8}");
                    break;
            }
        }
    }
}