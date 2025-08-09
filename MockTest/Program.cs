using System;

namespace MockOPCTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Mock OPC Server Test ===");
            Console.WriteLine("Testing OPC connection simulation\n");

            // Test basic OPC connection flow without COM dependencies
            TestOPCConnectionFlow();
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void TestOPCConnectionFlow()
        {
            try
            {
                Console.WriteLine("Step 1: Simulating OPC Server Connection...");
                var opcServer = new MockOPCServer();
                
                Console.WriteLine("Step 2: Connecting to 'National Instruments.NIOPCServer.V5'...");
                opcServer.Connect("National Instruments.NIOPCServer.V5");
                Console.WriteLine("✓ Connected successfully");

                Console.WriteLine("\nStep 3: Creating OPC Group...");
                var opcGroup = opcServer.CreateGroup("TestGroup");
                opcGroup.UpdateRate = 1000;
                opcGroup.IsActive = true;
                Console.WriteLine("✓ Group created successfully");

                Console.WriteLine("\nStep 4: Adding OPC Items...");
                opcGroup.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                opcGroup.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                Console.WriteLine("✓ Items added successfully");

                Console.WriteLine("\nStep 5: Reading OPC Data...");
                var data = opcGroup.SyncRead(new int[] { 1, 2 });
                
                Console.WriteLine("\nStep 6: Displaying Results:");
                Console.WriteLine("┌─────────────────────┬─────────────┬─────────────────┐");
                Console.WriteLine("│       Item Name     │    Value    │    Timestamp    │");
                Console.WriteLine("├─────────────────────┼─────────────┼─────────────────┤");
                Console.WriteLine($"│ AO0_Speed           │ {data[0],10} │ {DateTime.Now:HH:mm:ss.fff} │");
                Console.WriteLine($"│ AO1_Torque          │ {data[1],10} │ {DateTime.Now:HH:mm:ss.fff} │");
                Console.WriteLine("└─────────────────────┴─────────────┴─────────────────┘");

                Console.WriteLine("\nStep 7: Disconnecting...");
                opcServer.Disconnect();
                Console.WriteLine("✓ Disconnected successfully");

                Console.WriteLine("\n🎉 OPC Connection Test PASSED!");
                Console.WriteLine("All OPC operations completed successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ OPC Test FAILED: {ex.Message}");
            }
        }
    }

    // Simple Mock OPC Classes (no COM dependencies)
    public class MockOPCServer
    {
        private bool _connected = false;

        public void Connect(string serverName)
        {
            Console.WriteLine($"   → Connecting to {serverName}...");
            System.Threading.Thread.Sleep(500); // Simulate connection delay
            _connected = true;
        }

        public void Disconnect()
        {
            Console.WriteLine("   → Disconnecting from OPC Server...");
            _connected = false;
        }

        public MockOPCGroup CreateGroup(string groupName)
        {
            Console.WriteLine($"   → Creating group: {groupName}");
            return new MockOPCGroup(groupName);
        }
    }

    public class MockOPCGroup
    {
        private string _name;
        private System.Collections.Generic.List<MockOPCItem> _items;
        
        public int UpdateRate { get; set; }
        public bool IsActive { get; set; }

        public MockOPCGroup(string name)
        {
            _name = name;
            _items = new System.Collections.Generic.List<MockOPCItem>();
        }

        public void AddItem(string itemName, int handle)
        {
            Console.WriteLine($"   → Adding item: {itemName} (Handle: {handle})");
            _items.Add(new MockOPCItem(itemName, handle));
        }

        public object[] SyncRead(int[] handles)
        {
            Console.WriteLine($"   → Reading {handles.Length} items...");
            
            // Simulate realistic sensor data
            var random = new Random();
            object[] values = new object[handles.Length];
            
            for (int i = 0; i < handles.Length; i++)
            {
                if (i == 0) // AO0_Speed
                    values[i] = random.Next(1500, 3000); // RPM values
                else if (i == 1) // AO1_Torque  
                    values[i] = random.Next(50, 150); // Torque values
                else
                    values[i] = random.Next(0, 100);
            }
            
            System.Threading.Thread.Sleep(100); // Simulate read delay
            return values;
        }
    }

    public class MockOPCItem
    {
        public string ItemName { get; }
        public int Handle { get; }

        public MockOPCItem(string itemName, int handle)
        {
            ItemName = itemName;
            Handle = handle;
        }
    }
}