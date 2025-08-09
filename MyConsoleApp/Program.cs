using System;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MyConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OPC DA Continuous Reader ===");
            Console.WriteLine("Press 'q' and Enter to quit\n");
            
            // Test connection first
            if (!TestOPCConnection())
            {
                Console.WriteLine("✗ Cannot connect to NI OPC Server");
                Console.WriteLine("Please ensure NI OPC Server is running and OPC Core Components are installed");
                return;
            }
            
            Console.WriteLine("✓ Connected to NI OPC Server");
            Console.WriteLine("Starting continuous data reading...\n");
            
            // Start continuous reading
            StartContinuousReading();
        }
        
        static void StartContinuousReading()
        {
            int readCount = 0;
            
            Console.WriteLine("┌────────────────────────────────────────────────────────────────┐");
            Console.WriteLine("│                    LIVE OPC DATA READER                       │");
            Console.WriteLine("│                  Simple Loop - 2 Readings                     │");
            Console.WriteLine("└────────────────────────────────────────────────────────────────┘");
            Console.WriteLine();
            
            for (int i = 1; i <= 2; i++)
            {
                try
                {
                    readCount++;
                    DateTime now = DateTime.Now;
                    
                    Console.WriteLine($"Reading #{readCount:D4} - {now:yyyy-MM-dd HH:mm:ss.fff}");
                    Console.WriteLine("═══════════════════════════════════════════════════════════════");
                    
                    // Read OPC data
                    var data = ReadOPCData();
                    
                    if (data != null && data.Length >= 2)
                    {
                        Console.WriteLine($"│ AO0_Speed           │ {data[0],10} │ {now:HH:mm:ss.fff} │");
                        Console.WriteLine($"│ AO1_Torque          │ {data[1],10} │ {now:HH:mm:ss.fff} │");
                        Console.WriteLine("═══════════════════════════════════════════════════════════════");
                        Console.WriteLine($"Status: ✓ Connected | Readings: {readCount}");
                    }
                    else
                    {
                        Console.WriteLine($"│ Connection Error    │     N/A    │ {now:HH:mm:ss.fff} │");
                        Console.WriteLine("═══════════════════════════════════════════════════════════════");
                        Console.WriteLine($"Status: ✗ Error | Readings: {readCount}");
                    }
                    
                    Console.WriteLine();
                    
                    // Wait 1 second before next reading (except for last iteration)
                    if (i < 2)
                        System.Threading.Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in reading loop: {ex.Message}");
                    System.Threading.Thread.Sleep(1000);
                }
            }
            
            Console.WriteLine("Completed 2 readings.");
            Console.WriteLine("Program finished.");
        }
        
        static object[] ReadOPCData()
        {
            try
            {
                // Create OPC Server object
                Type opcServerType = Type.GetTypeFromProgID("OPC.Automation.1");
                if (opcServerType == null)
                {
                    Console.WriteLine("OPC Automation not found. Please install OPC Core Components.");
                    return null;
                }
                
                dynamic opcServer = Activator.CreateInstance(opcServerType);
                
                // Connect to NI OPC Server
                opcServer.Connect("National Instruments.NIOPCServer.V5");
                
                // Create OPC Group
                dynamic opcGroup = opcServer.OPCGroups.Add("Group1");
                opcGroup.UpdateRate = 1000;
                opcGroup.IsActive = true;
                opcGroup.IsSubscribed = true;
                
                // Add OPC Items
                dynamic opcItems = opcGroup.OPCItems;
                dynamic item1 = opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO0_Speed", 1);
                dynamic item2 = opcItems.AddItem("ADAM5000TCP.ADAM-5024.AO1_Torque", 2);
                
                // Read values
                object[] values = new object[2];
                object[] errors = new object[2];
                int[] handles = { 1, 2 };
                
                opcGroup.SyncRead((short)1, 2, handles, out values, out errors);
                
                // Cleanup
                opcServer.Disconnect();
                
                return values;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OPC Error: {ex.Message}");
                return null;
            }
        }
        
        static bool TestOPCConnection()
        {
            try
            {
                Type opcServerType = Type.GetTypeFromProgID("OPC.Automation.1");
                if (opcServerType == null)
                    return false;
                
                dynamic opcServer = Activator.CreateInstance(opcServerType);
                opcServer.Connect("National Instruments.NIOPCServer.V5");
                opcServer.Disconnect();
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        static void TryDirectModbusRead()
        {
            Console.WriteLine("\n=== Testing Direct Modbus TCP Connection ===");
            try
            {
                using (var client = new TcpClient())
                {
                    client.Connect("localhost", 32405);
                    var stream = client.GetStream();
                    
                    Console.WriteLine("✓ Connected to NI OPC Server on port 32405");
                    Console.WriteLine("Reading Modbus registers 400041 (AO0_Speed) and 400042 (AO1_Torque)...");
                    
                    // Read holding registers 400041 and 400042
                    byte[] request = CreateModbusReadRequest(1, 40040, 2); // Function code 3, start from register 40040, read 2 registers
                    stream.Write(request, 0, request.Length);
                    
                    byte[] response = new byte[13];
                    int bytesRead = stream.Read(response, 0, response.Length);
                    
                    if (bytesRead >= 9)
                    {
                        // Parse Modbus response
                        ushort reg1 = (ushort)((response[9] << 8) | response[10]); // Register 400041 (AO0_Speed)
                        ushort reg2 = (ushort)((response[11] << 8) | response[12]); // Register 400042 (AO1_Torque)
                        
                        Console.WriteLine($"✓ Successfully read Modbus data:");
                        Console.WriteLine($"  AO0_Speed (400041): {reg1}");
                        Console.WriteLine($"  AO1_Torque (400042): {reg2}");
                    }
                    else
                    {
                        Console.WriteLine("✗ Invalid Modbus response");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Modbus TCP read failed: {ex.Message}");
            }
        }
        
        static byte[] CreateModbusReadRequest(byte unitId, ushort startAddress, ushort quantity)
        {
            byte[] request = new byte[12];
            
            // MBAP Header
            request[0] = 0x00; // Transaction ID high
            request[1] = 0x01; // Transaction ID low
            request[2] = 0x00; // Protocol ID high
            request[3] = 0x00; // Protocol ID low
            request[4] = 0x00; // Length high
            request[5] = 0x06; // Length low
            request[6] = unitId; // Unit ID
            
            // PDU
            request[7] = 0x03; // Function code (Read Holding Registers)
            request[8] = (byte)(startAddress >> 8); // Start address high
            request[9] = (byte)(startAddress & 0xFF); // Start address low
            request[10] = (byte)(quantity >> 8); // Quantity high
            request[11] = (byte)(quantity & 0xFF); // Quantity low
            
            return request;
        }
    }
}
