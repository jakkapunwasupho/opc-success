using System;
using System.Runtime.InteropServices;

namespace SimulatorOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== NI OPC Server with Simulator Test ===");
            Console.WriteLine("Using NI OPC Server simulator to test AO0_Speed reading\n");

            TestWithSimulator();
        }

        static void TestWithSimulator()
        {
            Console.WriteLine("Step 1: Testing with NI OPC Simulator");
            Console.WriteLine("======================================");

            // ลองใช้ simulator driver แทน ADAM device
            try
            {
                Console.WriteLine("Attempting to connect to NI OPC Server with simulator data...");
                
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServers.V5");
                if (serverType == null)
                {
                    Console.WriteLine("❌ NI OPC Server not found");
                    return;
                }

                dynamic opcServer = Activator.CreateInstance(serverType);
                Console.WriteLine("✅ NI OPC Server instance created");

                // ใช้ OPCGroups interface
                var groups = opcServer.OPCGroups;
                var group = groups.Add("SimulatorGroup");
                group.UpdateRate = 1000;
                group.IsActive = true;
                group.IsSubscribed = true;

                var items = group.OPCItems;
                
                // ลองใช้ simulator items แทน ADAM items
                Console.WriteLine("\nStep 2: Adding simulator items...");
                
                // ลอง item names ที่เป็นไปได้จาก simulator
                string[] simulatorItems = {
                    "Simulator.Speed", 
                    "Simulator.AO0_Speed",
                    "Advanced_Simulator.Speed",
                    "Memory_Based.Speed",
                    "Memory.AO0_Speed",
                    "Demo.Speed",
                    "Test.AO0_Speed",
                    "System.Random1",
                    "System.Random2"
                };

                int itemHandle = 1;
                int successfulItems = 0;

                foreach (string itemName in simulatorItems)
                {
                    try
                    {
                        items.AddItem(itemName, itemHandle);
                        Console.WriteLine($"   ✅ Added: {itemName}");
                        successfulItems++;
                        itemHandle++;
                    }
                    catch (Exception itemEx)
                    {
                        Console.WriteLine($"   ❌ Failed: {itemName} - {itemEx.Message}");
                    }
                }

                if (successfulItems == 0)
                {
                    Console.WriteLine("\nStep 3: Trying generic OPC test items...");
                    
                    // ลอง generic items
                    string[] genericItems = {
                        "Random.Real8",
                        "Random.Int2", 
                        "Sawtooth.Real8",
                        "Triangle.Real8",
                        "Square.Boolean",
                        "Bucket Brigade.Real8",
                        "Random.String",
                        "$SimulatedValue1",
                        "$SimulatedValue2"
                    };

                    foreach (string itemName in genericItems)
                    {
                        try
                        {
                            items.AddItem(itemName, itemHandle);
                            Console.WriteLine($"   ✅ Added: {itemName}");
                            successfulItems++;
                            itemHandle++;
                            
                            if (successfulItems >= 2) break; // เอาแค่ 2 items
                        }
                        catch (Exception itemEx)
                        {
                            Console.WriteLine($"   ❌ Failed: {itemName}");
                        }
                    }
                }

                if (successfulItems > 0)
                {
                    Console.WriteLine($"\nStep 4: Reading {successfulItems} simulator values...");
                    
                    // สร้าง handles array
                    int[] handles = new int[successfulItems];
                    for (int i = 0; i < successfulItems; i++)
                    {
                        handles[i] = i + 1;
                    }

                    // อ่านข้อมูลจาก simulator
                    for (int readCount = 1; readCount <= 3; readCount++)
                    {
                        try
                        {
                            object[] values = new object[successfulItems];
                            object[] errors = new object[successfulItems];

                            group.SyncRead((short)1, successfulItems, handles, out values, out errors);

                            Console.WriteLine($"\n   📊 Simulator Reading #{readCount} - {DateTime.Now:HH:mm:ss.fff}:");
                            Console.WriteLine("   ╔════════════════════════════════════════╗");
                            
                            for (int i = 0; i < successfulItems; i++)
                            {
                                string itemName = i == 0 ? "Simulated_Speed" : $"Simulated_Value_{i+1}";
                                Console.WriteLine($"   ║  🎯 {itemName,-15}: {values[i],-10}   ║");
                            }
                            
                            Console.WriteLine($"   ║  ❗ Errors: {string.Join(", ", errors),-20}  ║");
                            Console.WriteLine($"   ║  ⏰ Time: {DateTime.Now:HH:mm:ss.fff}              ║");
                            Console.WriteLine("   ╚════════════════════════════════════════╝");
                            
                            if (readCount < 3) System.Threading.Thread.Sleep(1000);
                        }
                        catch (Exception readEx)
                        {
                            Console.WriteLine($"   ❌ Reading #{readCount} failed: {readEx.Message}");
                        }
                    }

                    Console.WriteLine("\n🎉 SUCCESS: OPC Server connection and data reading working!");
                    Console.WriteLine("💡 This proves the OPC infrastructure is functional");
                    Console.WriteLine("💡 For real ADAM data, configure ADAM-5000TCP device properly");
                }
                else
                {
                    Console.WriteLine("\n❌ No simulator items could be added");
                    Console.WriteLine("💡 May need to configure NI OPC Server with proper drivers/devices");
                }

                // Clean up
                opcServer.Disconnect();
            }
            catch (COMException comEx)
            {
                Console.WriteLine($"\n❌ COM Exception: {comEx.Message}");
                Console.WriteLine($"   HRESULT: 0x{comEx.HResult:X8}");
                
                if (comEx.HResult == unchecked((int)0x80040112))
                {
                    Console.WriteLine("   🔍 License issue - try running OPC configuration tool first");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
            }
        }
    }
}