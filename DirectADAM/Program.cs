using System;
using System.Net.Sockets;
using System.Text;

namespace DirectADAMTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Direct ADAM-5024 Connection Test ===");
            Console.WriteLine("Bypassing OPC Server - connecting directly to ADAM-5024 device\n");

            TestDirectADAMConnection();
        }

        static void TestDirectADAMConnection()
        {
            Console.WriteLine("Method 1: Direct TCP connection to ADAM-5024");
            Console.WriteLine("=============================================");
            
            // ADAM-5024 connection parameters (from NI OPC configuration)
            string[] adamIPs = {
                "localhost"       // Based on your configuration: localhost:1
            };
            
            int[] adamPorts = {
                502,   // Standard Modbus TCP
                1502,  // Alternative Modbus TCP  
                5020,  // NI OPC Modbus
                32405  // NI OPC Server port
            };
            
            foreach (string ip in adamIPs)
            {
                foreach (int port in adamPorts)
                {
                    if (TryConnectADAM(ip, port))
                    {
                        Console.WriteLine($"🎉 Found ADAM-5024 at {ip}:{port}!");
                        ReadADAMData(ip, port);
                        return;
                    }
                }
            }
            
            Console.WriteLine("❌ Could not find ADAM-5024 device on network");
            Console.WriteLine("💡 ADAM device might not be connected or configured");
            
            Console.WriteLine("\nMethod 2: Simulate ADAM-5024 responses");
            Console.WriteLine("======================================");
            SimulateADAMResponses();
        }

        static bool TryConnectADAM(string ip, int port)
        {
            try
            {
                Console.WriteLine($"Testing {ip}:{port}...");
                
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(ip, port);
                    
                    Console.WriteLine($"✅ Connected to {ip}:{port}");
                    return true;
                }
            }
            catch
            {
                Console.WriteLine($"❌ No response from {ip}:{port}");
                return false;
            }
        }

        static void ReadADAMData(string ip, int port)
        {
            try
            {
                Console.WriteLine($"\nReading data from ADAM-5024 at {ip}:{port}");
                Console.WriteLine("============================================");
                
                using (var client = new TcpClient())
                {
                    client.Connect(ip, port);
                    var stream = client.GetStream();
                    
                    // Method 1: Modbus TCP request for AO0_Speed
                    if (port == 502 || port == 1502)
                    {
                        ReadModbusData(stream, ip, port);
                    }
                    // Method 2: ADAM ASCII protocol
                    else if (port == 5040 || port == 10001)
                    {
                        ReadADAMAsciiData(stream, ip, port);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Failed to read data from {ip}:{port}: {ex.Message}");
            }
        }

        static void ReadModbusData(NetworkStream stream, string ip, int port)
        {
            Console.WriteLine($"Using Modbus TCP protocol on {ip}:{port}");
            
            try
            {
                // Modbus request for holding registers (AO channels)
                // Function Code 03 (Read Holding Registers)
                // Starting Address: 0 (AO0)
                // Quantity: 2 (AO0, AO1)
                
                byte[] modbusRequest = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    0x03,       // Function Code (Read Holding Registers)
                    0x00, 0x00, // Starting Address
                    0x00, 0x02  // Quantity (2 registers)
                };
                
                stream.Write(modbusRequest, 0, modbusRequest.Length);
                Console.WriteLine("📡 Sent Modbus request for AO0_Speed and AO1_Torque");
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead >= 9)
                {
                    // Parse Modbus response
                    ushort ao0Value = (ushort)((response[9] << 8) | response[10]);
                    ushort ao1Value = (ushort)((response[11] << 8) | response[12]);
                    
                    // Convert to engineering units (assuming 0-10V = 0-10000 units)
                    float ao0_Speed = (ao0Value / 10000.0f) * 3000; // Scale to RPM
                    float ao1_Torque = (ao1Value / 10000.0f) * 200; // Scale to Nm
                    
                    Console.WriteLine("📊 DIRECT ADAM-5024 DATA:");
                    Console.WriteLine("╔═══════════════════════════════════════╗");
                    Console.WriteLine($"║ 🎯 AO0_Speed:  {ao0_Speed:F1} RPM          ║");
                    Console.WriteLine($"║ ⚡ AO1_Torque: {ao1_Torque:F1} Nm           ║");
                    Console.WriteLine($"║ 📡 Source:     Direct Modbus TCP     ║");
                    Console.WriteLine($"║ 🔧 Raw AO0:   {ao0Value}                  ║");
                    Console.WriteLine($"║ 🔧 Raw AO1:   {ao1Value}                  ║");
                    Console.WriteLine("╚═══════════════════════════════════════╝");
                }
                else
                {
                    Console.WriteLine($"❌ Invalid Modbus response: {bytesRead} bytes");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Modbus read failed: {ex.Message}");
            }
        }

        static void ReadADAMAsciiData(NetworkStream stream, string ip, int port)
        {
            Console.WriteLine($"Using ADAM ASCII protocol on {ip}:{port}");
            
            try
            {
                // ADAM ASCII command to read analog inputs
                // Format: #AA (Address Command)
                string command = "#01\r"; // Read from address 01
                byte[] commandBytes = Encoding.ASCII.GetBytes(command);
                
                stream.Write(commandBytes, 0, commandBytes.Length);
                Console.WriteLine($"📡 Sent ADAM command: {command.Trim()}");
                
                byte[] response = new byte[256];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead > 0)
                {
                    string responseStr = Encoding.ASCII.GetString(response, 0, bytesRead);
                    Console.WriteLine($"📡 ADAM Response: {responseStr}");
                    
                    // Parse ADAM response (format varies by model)
                    ParseADAMResponse(responseStr);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ADAM ASCII read failed: {ex.Message}");
            }
        }

        static void ParseADAMResponse(string response)
        {
            try
            {
                // ADAM-5024 typically returns analog output values
                // Format might be: >+01.234+02.567 (for 2 channels)
                
                if (response.StartsWith(">"))
                {
                    string data = response.Substring(1).Trim();
                    string[] values = data.Split(new char[] { '+', '-' }, StringSplitOptions.RemoveEmptyEntries);
                    
                    if (values.Length >= 2)
                    {
                        if (float.TryParse(values[0], out float ao0) && 
                            float.TryParse(values[1], out float ao1))
                        {
                            // Convert to engineering units
                            float speed = ao0 * 300; // Example conversion
                            float torque = ao1 * 20;  // Example conversion
                            
                            Console.WriteLine("📊 PARSED ADAM-5024 DATA:");
                            Console.WriteLine("╔═══════════════════════════════════════╗");
                            Console.WriteLine($"║ 🎯 AO0_Speed:  {speed:F1} RPM          ║");
                            Console.WriteLine($"║ ⚡ AO1_Torque: {torque:F1} Nm           ║");
                            Console.WriteLine($"║ 📡 Source:     Direct ADAM ASCII     ║");
                            Console.WriteLine($"║ 🔧 Raw AO0:   {ao0:F3}                ║");
                            Console.WriteLine($"║ 🔧 Raw AO1:   {ao1:F3}                ║");
                            Console.WriteLine("╚═══════════════════════════════════════╝");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"📡 Raw ADAM response: {response}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Response parsing failed: {ex.Message}");
            }
        }

        static void SimulateADAMResponses()
        {
            Console.WriteLine("Generating simulated ADAM-5024 responses...");
            Console.WriteLine("(This shows what real data would look like)\n");
            
            var random = new Random();
            
            for (int i = 1; i <= 5; i++)
            {
                // Simulate realistic motor data
                float ao0_Speed = 1500 + random.Next(0, 1500);    // 1500-3000 RPM
                float ao1_Torque = 50 + random.Next(0, 100);      // 50-150 Nm
                
                Console.WriteLine($"📊 Simulated Reading #{i} - {DateTime.Now:HH:mm:ss.fff}");
                Console.WriteLine("╔═══════════════════════════════════════╗");
                Console.WriteLine($"║ 🎯 AO0_Speed:  {ao0_Speed:F1} RPM          ║");
                Console.WriteLine($"║ ⚡ AO1_Torque: {ao1_Torque:F1} Nm           ║");
                Console.WriteLine($"║ 📡 Source:     Simulated ADAM-5024   ║");
                Console.WriteLine($"║ 🔧 Quality:   Good                   ║");
                Console.WriteLine("╚═══════════════════════════════════════╝\n");
                
                if (i < 5) System.Threading.Thread.Sleep(1000);
            }
            
            Console.WriteLine("💡 This demonstrates the expected data format");
            Console.WriteLine("💡 Real ADAM-5024 would provide similar values");
        }
    }
}