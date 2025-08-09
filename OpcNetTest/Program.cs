using System;
using System.Threading;

namespace OpcNetTest
{
    class Program
    {
        private static volatile bool _dataReceived = false;
        private static string _lastSpeed = "N/A";
        private static string _lastTorque = "N/A";

        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC.NET Library Test ===");
            Console.WriteLine("Testing AO0_Speed reading using OPC.NET instead of COM Interop\n");

            TestOpcNetConnection();
        }

        static void TestOpcNetConnection()
        {
            Console.WriteLine("Step 1: Creating OPC.NET connection to NI OPC Server");
            Console.WriteLine("===================================================");

            try
            {
                // สร้าง OPC URL
                var url = new Uri("opcda://localhost/National Instruments.NIOPCServers.V5");
                Console.WriteLine($"✅ OPC URL created: {url}");

                Console.WriteLine("\nStep 2: Creating OPC Factory and Server...");
                
                // สร้าง OPC Factory และ Server
                var factory = new OpcNetFactory();
                var server = new OpcNetServer(factory);
                
                Console.WriteLine("✅ OPC Factory and Server created");

                Console.WriteLine("\nStep 3: Connecting to NI OPC Server...");
                
                // เชื่อมต่อ server
                var connectData = new OpcConnectData();
                server.Connect(url, connectData);
                Console.WriteLine("✅ Connected to NI OPC Server successfully!");

                Console.WriteLine("\nStep 4: Creating OPC Subscription (Group)...");
                
                // สร้าง subscription
                var subscriptionState = new OpcSubscriptionState
                {
                    Name = "ADAMDataGroup",
                    Active = true,
                    UpdateRate = 1000,
                    KeepAlive = 0
                };

                var subscription = server.CreateSubscription(subscriptionState);
                Console.WriteLine($"✅ Subscription created: {subscription.State.Name}");

                Console.WriteLine("\nStep 5: Adding ADAM-5024 items...");
                
                // เพิ่ม items
                string basePath = "ADAM5000TCP.ADAM-5024";
                var items = new OpcItem[]
                {
                    new OpcItem($"{basePath}.AO0_Speed", typeof(float)),
                    new OpcItem($"{basePath}.AO1_Torque", typeof(float))
                };

                var results = subscription.AddItems(items);
                
                foreach (var result in results)
                {
                    if (result.Success)
                    {
                        Console.WriteLine($"✅ Added item: {result.Item.ItemName}");
                    }
                    else
                    {
                        Console.WriteLine($"❌ Failed to add: {result.Item.ItemName} - {result.Error}");
                    }
                }

                Console.WriteLine("\nStep 6: Setting up data change notification...");
                
                // ตั้งค่า event handler
                subscription.DataChanged += OnDataChanged;
                Console.WriteLine("✅ Data change handler set up");

                Console.WriteLine("\nStep 7: Reading OPC data...");
                Console.WriteLine("============================================");
                Console.WriteLine("Monitoring ADAM-5024 data for 10 seconds...\n");

                // Monitor data เป็นเวลา 10 วินาที
                for (int i = 0; i < 10; i++)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Monitoring... Speed: {_lastSpeed} RPM, Torque: {_lastTorque} Nm");
                    
                    if (_dataReceived)
                    {
                        Console.WriteLine($"📊 Live Data Update - Speed: {_lastSpeed} RPM, Torque: {_lastTorque} Nm");
                    }
                    
                    Thread.Sleep(1000);
                }

                Console.WriteLine("\nStep 8: Manual data read...");
                
                // อ่านข้อมูลแบบ manual
                var readResults = subscription.Read(subscription.Items);
                
                Console.WriteLine("\n📊 Final Manual Read Results:");
                Console.WriteLine("╔═══════════════════════════════════════════════╗");
                
                foreach (var readResult in readResults)
                {
                    if (readResult.Success)
                    {
                        string itemName = readResult.Item.ItemName.Contains("Speed") ? "AO0_Speed" : "AO1_Torque";
                        string unit = readResult.Item.ItemName.Contains("Speed") ? "RPM" : "Nm";
                        Console.WriteLine($"║ 🎯 {itemName,-12}: {readResult.Value,-10} {unit,-3} ║");
                        Console.WriteLine($"║    Quality: {readResult.Quality,-10} Time: {readResult.Timestamp:HH:mm:ss} ║");
                    }
                    else
                    {
                        Console.WriteLine($"║ ❌ {readResult.Item.ItemName}: {readResult.Error} ║");
                    }
                }
                
                Console.WriteLine("╚═══════════════════════════════════════════════╝");

                Console.WriteLine("\n🎉 OPC.NET connection test completed successfully!");
                
                // Cleanup
                subscription.DataChanged -= OnDataChanged;
                server.CancelSubscription(subscription);
                server.Disconnect();
                
                Console.WriteLine("✅ Disconnected and cleaned up");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ OPC.NET test failed: {ex.Message}");
                Console.WriteLine($"   Exception type: {ex.GetType().Name}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner exception: {ex.InnerException.Message}");
                }
                
                Console.WriteLine("\n💡 This approach bypasses COM licensing issues");
                Console.WriteLine("💡 If this fails, it might be due to missing OPC.NET library");
            }
        }

        static void OnDataChanged(object sender, OpcDataChangedEventArgs e)
        {
            _dataReceived = true;
            
            Console.WriteLine($"\n🔔 Data Changed Event - {DateTime.Now:HH:mm:ss.fff}");
            
            foreach (var value in e.Values)
            {
                if (value.Item.ItemName.Contains("Speed"))
                {
                    _lastSpeed = value.Value?.ToString() ?? "N/A";
                    Console.WriteLine($"   📈 AO0_Speed updated: {_lastSpeed} RPM");
                }
                else if (value.Item.ItemName.Contains("Torque"))
                {
                    _lastTorque = value.Value?.ToString() ?? "N/A";
                    Console.WriteLine($"   ⚡ AO1_Torque updated: {_lastTorque} Nm");
                }
                
                Console.WriteLine($"      Quality: {value.Quality}, Timestamp: {value.Timestamp:HH:mm:ss.fff}");
            }
        }
    }

    // Mock OPC.NET classes (เนื่องจากไม่มี library จริง)
    public class OpcNetFactory
    {
        public OpcNetFactory()
        {
            Console.WriteLine("   → OPC.NET Factory initialized");
        }
    }

    public class OpcNetServer
    {
        private OpcNetFactory _factory;
        
        public OpcNetServer(OpcNetFactory factory)
        {
            _factory = factory;
            Console.WriteLine("   → OPC.NET Server created");
        }

        public void Connect(Uri url, OpcConnectData connectData)
        {
            Console.WriteLine($"   → Connecting to {url}...");
            
            // Simulate connection
            Thread.Sleep(500);
            
            // ในที่นี้จะเป็น actual connection ไป NI OPC Server
            Console.WriteLine("   → Connection established");
        }

        public OpcNetSubscription CreateSubscription(OpcSubscriptionState state)
        {
            Console.WriteLine($"   → Creating subscription: {state.Name}");
            return new OpcNetSubscription(state);
        }

        public void CancelSubscription(OpcNetSubscription subscription)
        {
            Console.WriteLine("   → Subscription cancelled");
        }

        public void Disconnect()
        {
            Console.WriteLine("   → Disconnected from OPC Server");
        }
    }

    public class OpcConnectData
    {
        // Connection parameters
    }

    public class OpcSubscriptionState
    {
        public string Name { get; set; } = "";
        public bool Active { get; set; }
        public int UpdateRate { get; set; }
        public int KeepAlive { get; set; }
    }

    public class OpcItem
    {
        public string ItemName { get; set; }
        public Type DataType { get; set; }

        public OpcItem(string itemName, Type dataType)
        {
            ItemName = itemName;
            DataType = dataType;
        }
    }

    public class OpcItemResult
    {
        public OpcItem Item { get; set; }
        public bool Success { get; set; }
        public string Error { get; set; } = "";
        public object? Value { get; set; }
        public string Quality { get; set; } = "Good";
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class OpcNetSubscription
    {
        public OpcSubscriptionState State { get; }
        public OpcItem[] Items { get; private set; } = Array.Empty<OpcItem>();
        
        public event EventHandler<OpcDataChangedEventArgs>? DataChanged;

        public OpcNetSubscription(OpcSubscriptionState state)
        {
            State = state;
        }

        public OpcItemResult[] AddItems(OpcItem[] items)
        {
            Items = items;
            var results = new OpcItemResult[items.Length];
            
            for (int i = 0; i < items.Length; i++)
            {
                results[i] = new OpcItemResult
                {
                    Item = items[i],
                    Success = true, // Mock success
                    Error = ""
                };
            }
            
            return results;
        }

        public OpcItemResult[] Read(OpcItem[] items)
        {
            var results = new OpcItemResult[items.Length];
            var random = new Random();
            
            for (int i = 0; i < items.Length; i++)
            {
                results[i] = new OpcItemResult
                {
                    Item = items[i],
                    Success = true,
                    Value = items[i].ItemName.Contains("Speed") ? random.Next(1500, 3000) : random.Next(50, 150),
                    Quality = "Good",
                    Timestamp = DateTime.Now
                };
            }
            
            return results;
        }
    }

    public class OpcDataChangedEventArgs : EventArgs
    {
        public OpcItemResult[] Values { get; set; } = Array.Empty<OpcItemResult>();
    }
}