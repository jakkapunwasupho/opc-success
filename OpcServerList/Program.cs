using System;
using System.Runtime.InteropServices;

namespace OpcServerList
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC Server List Test ===");
            Console.WriteLine("Finding available OPC servers on this machine\n");

            ListOPCServers();
            TestFoundServers();
        }

        static void ListOPCServers()
        {
            Console.WriteLine("Step 1: Using OPC.ServerList to find registered OPC servers");
            Console.WriteLine("===========================================================");

            try
            {
                // สร้าง OPC Server List object
                Type serverListType = Type.GetTypeFromProgID("OPC.ServerList.1");
                if (serverListType == null)
                {
                    serverListType = Type.GetTypeFromProgID("OPC.ServerList");
                }

                if (serverListType == null)
                {
                    Console.WriteLine("❌ OPC.ServerList not found in registry");
                    return;
                }

                dynamic serverList = Activator.CreateInstance(serverListType);
                Console.WriteLine("✅ OPC.ServerList created successfully");

                Console.WriteLine("\nStep 2: Enumerating OPC DA Servers...");

                // OPC DA Server Category CLSID
                string opcDaCatId = "{63D5F432-CFE4-11D1-B2C8-0060083BA1FB}";
                
                try
                {
                    // Get list of OPC DA servers
                    var servers = serverList.GetCLSIDsFromCategoryID(opcDaCatId);
                    
                    if (servers != null)
                    {
                        Console.WriteLine($"Found {servers.Length} OPC DA servers:");
                        
                        for (int i = 0; i < servers.Length; i++)
                        {
                            string clsid = servers[i];
                            Console.WriteLine($"   [{i+1}] CLSID: {clsid}");
                            
                            // ลองหา ProgID จาก CLSID
                            try
                            {
                                string progId = serverList.GetProgIDFromCLSID(clsid);
                                Console.WriteLine($"       ProgID: {progId}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"       ProgID: Not found ({ex.Message})");
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine("No OPC DA servers found");
                    }
                }
                catch (Exception enumEx)
                {
                    Console.WriteLine($"❌ Error enumerating servers: {enumEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        static void TestFoundServers()
        {
            Console.WriteLine("\nStep 3: Testing direct connection to known ProgIDs");
            Console.WriteLine("==================================================");

            string[] knownProgIds = {
                "OPC.Automation.1",
                "Matrikon.OPC.Automation.1", 
                "National Instruments.NIOPCServers.V5",
                "Instruments.NIOPCServers_AE.V5"
            };

            foreach (string progId in knownProgIds)
            {
                TestSingleProgId(progId);
            }
        }

        static void TestSingleProgId(string progId)
        {
            try
            {
                Console.WriteLine($"\nTesting: {progId}");
                
                Type serverType = Type.GetTypeFromProgID(progId);
                if (serverType == null)
                {
                    Console.WriteLine($"   ❌ ProgID not found");
                    return;
                }

                Console.WriteLine($"   ✅ ProgID found in registry");
                
                // ลองสร้าง instance
                try
                {
                    dynamic serverObj = Activator.CreateInstance(serverType);
                    Console.WriteLine($"   ✅ Instance created successfully");
                    
                    // ลองเชื่อมต่อ
                    if (progId == "OPC.Automation.1" || progId == "Matrikon.OPC.Automation.1")
                    {
                        // นี่คือ OPC Automation wrapper
                        try
                        {
                            serverObj.Connect("National Instruments.NIOPCServers.V5");
                            Console.WriteLine($"   🎯 Connected to NI OPC Server via {progId}!");
                            
                            // ลองอ่านข้อมูล
                            if (TryReadDataViaAutomation(serverObj))
                            {
                                Console.WriteLine($"   🎉 Successfully read AO0_Speed data via {progId}!");
                            }
                            
                            serverObj.Disconnect();
                        }
                        catch (Exception connectEx)
                        {
                            Console.WriteLine($"   ❌ Connection failed: {connectEx.Message}");
                        }
                    }
                    else
                    {
                        // นี่คือ OPC Server โดยตรง
                        Console.WriteLine($"   💡 This is a direct OPC Server (needs different connection method)");
                    }
                }
                catch (COMException comEx)
                {
                    Console.WriteLine($"   ❌ COM Exception: {comEx.Message}");
                    
                    if (comEx.HResult == unchecked((int)0x80040112))
                    {
                        Console.WriteLine($"   🔍 Class not licensed - this is normal for some OPC servers");
                    }
                }
                catch (Exception createEx)
                {
                    Console.WriteLine($"   ❌ Create instance failed: {createEx.Message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Test failed: {ex.Message}");
            }
        }

        static bool TryReadDataViaAutomation(dynamic opcServer)
        {
            try
            {
                var groups = opcServer.OPCGroups;
                var group = groups.Add("TestGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;
                
                var items = group.OPCItems;
                items.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                items.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                
                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };
                
                group.SyncRead((short)1, 2, handles, out values, out errors);
                
                Console.WriteLine($"      📊 AO0_Speed: {values[0]} RPM");
                Console.WriteLine($"      ⚡ AO1_Torque: {values[1]} Nm");
                Console.WriteLine($"      ❗ Errors: [{errors[0]}, {errors[1]}]");
                
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"      ❌ Data read failed: {ex.Message}");
                return false;
            }
        }
    }
}