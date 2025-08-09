using System;

namespace RealOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Testing NI OPC Server Connection ===");
            Console.WriteLine("Attempting to read AO0_Speed from real OPC Server\n");

            // ทดสอบการเชื่อมต่อ OPC จริง
            if (TestRealOPCConnection())
            {
                Console.WriteLine("🎉 Successfully connected and read real OPC data!");
            }
            else
            {
                Console.WriteLine("❌ Failed to connect to real OPC Server");
                Console.WriteLine("\nPossible solutions:");
                Console.WriteLine("1. Install OPC Core Components");
                Console.WriteLine("2. Start NI OPC Server service");
                Console.WriteLine("3. Configure ADAM-5000TCP device");
                Console.WriteLine("4. Check Windows firewall settings");
            }
        }

        static bool TestRealOPCConnection()
        {
            try
            {
                Console.WriteLine("Step 1: Checking OPC Automation availability...");
                
                // ตรวจสอบ OPC Automation
                Type opcServerType = Type.GetTypeFromProgID("OPC.Automation.1");
                if (opcServerType == null)
                {
                    Console.WriteLine("❌ OPC.Automation.1 not found");
                    Console.WriteLine("Need to install OPC Core Components from:");
                    Console.WriteLine("https://www.microsoft.com/en-us/download/details.aspx?id=3374");
                    return false;
                }
                
                Console.WriteLine("✅ OPC Automation available");
                
                Console.WriteLine("\nStep 2: Creating OPC Server instance...");
                dynamic opcServer = Activator.CreateInstance(opcServerType);
                
                Console.WriteLine("\nStep 3: Connecting to NI OPC Server...");
                opcServer.Connect("National Instruments.NIOPCServer.V5");
                Console.WriteLine("✅ Connected to National Instruments.NIOPCServer.V5");
                
                Console.WriteLine("\nStep 4: Creating OPC Group...");
                dynamic opcGroup = opcServer.OPCGroups.Add("RealDataGroup");
                opcGroup.UpdateRate = 1000;
                opcGroup.IsActive = true;
                opcGroup.IsSubscribed = true;
                
                Console.WriteLine("\nStep 5: Adding OPC Items...");
                dynamic opcItems = opcGroup.OPCItems;
                dynamic item1 = opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                dynamic item2 = opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                
                Console.WriteLine("✅ Items added:");
                Console.WriteLine("   - AO0_Speed (Handle: 1)");
                Console.WriteLine("   - AO1_Torque (Handle: 2)");
                
                Console.WriteLine("\nStep 6: Reading real OPC data...");
                
                // อ่านข้อมูลจริง 5 ครั้ง
                for (int i = 1; i <= 5; i++)
                {
                    object[] values = new object[2];
                    object[] errors = new object[2];
                    int[] handles = { 1, 2 };
                    
                    opcGroup.SyncRead((short)1, 2, handles, out values, out errors);
                    
                    DateTime now = DateTime.Now;
                    
                    Console.WriteLine($"\n📊 Reading #{i} - {now:HH:mm:ss.fff}");
                    Console.WriteLine($"   🔧 AO0_Speed: {values[0]} RPM");
                    Console.WriteLine($"   ⚡ AO1_Torque: {values[1]} Nm");
                    Console.WriteLine($"   ❗ Error Codes: [{errors[0]}, {errors[1]}]");
                    
                    if (i < 5)
                        System.Threading.Thread.Sleep(1000); // รอ 1 วินาที
                }
                
                Console.WriteLine("\nStep 7: Disconnecting...");
                opcServer.Disconnect();
                Console.WriteLine("✅ Disconnected successfully");
                
                return true;
            }
            catch (System.Runtime.InteropServices.COMException comEx)
            {
                Console.WriteLine($"\n❌ COM Error: {comEx.Message}");
                Console.WriteLine($"   Error Code: 0x{comEx.ErrorCode:X8}");
                
                if (comEx.ErrorCode == -2147221164) // 0x800401F4
                {
                    Console.WriteLine("   🔍 This error suggests OPC Server is not registered or running");
                    Console.WriteLine("   💡 Solution: Register NI OPC Server and ensure it's running");
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ General Error: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
                return false;
            }
        }
    }
}