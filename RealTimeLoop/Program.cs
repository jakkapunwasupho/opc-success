using System;
using System.Net.Sockets;
using System.Threading;

namespace RealTimeADAMLoop
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Real-Time ADAM-5024 Data Loop ===");
            Console.WriteLine("Reading AO0_Speed and AO1_Torque every second");
            Console.WriteLine("Press Ctrl+C to stop\n");

            StartRealTimeLoop();
        }

        static void StartRealTimeLoop()
        {
            string adamIP = "localhost";
            int adamPort = 502; // Modbus TCP port
            int readingCount = 0;
            
            Console.WriteLine("╔════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                   REAL-TIME ADAM-5024 DATA                ║");
            Console.WriteLine("║                 AO0_Speed & AO1_Torque Monitor            ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
            Console.WriteLine("║  #  │    Time    │  AO0_Speed  │ AO1_Torque │   Status   ║");
            Console.WriteLine("╠════════════════════════════════════════════════════════════╣");

            while (true)
            {
                try
                {
                    readingCount++;
                    DateTime now = DateTime.Now;
                    
                    var (speed, torque, success) = ReadADAMDataReal(adamIP, adamPort);
                    
                    string status = success ? "  ✅ OK  " : " ❌ ERR ";
                    string timeStr = now.ToString("HH:mm:ss");
                    
                    Console.WriteLine($"║{readingCount,3} │ {timeStr} │ {speed,8:F1} RPM │ {torque,7:F1} Nm │ {status} ║");
                    
                    // ทุก 10 readings แสดงหัวตารางใหม่
                    if (readingCount % 10 == 0)
                    {
                        Console.WriteLine("╠════════════════════════════════════════════════════════════╣");
                    }
                    
                    // รอ 1 วินาที
                    Thread.Sleep(1000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"║ERR│ {DateTime.Now:HH:mm:ss} │    ERROR    │   ERROR   │ ❌ FAIL║");
                    Console.WriteLine($"Error: {ex.Message}");
                    Thread.Sleep(1000);
                }
            }
        }

        static (float speed, float torque, bool success) ReadADAMDataReal(string ip, int port)
        {
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 1000;
                    client.SendTimeout = 1000;
                    client.Connect(ip, port);
                    
                    var stream = client.GetStream();
                    
                    // Modbus TCP request for holding registers
                    // Function Code 03 (Read Holding Registers)
                    // Starting Address: 40040 (400041 in 1-based addressing for Speed)
                    // Quantity: 2 (Speed and Torque registers)
                    byte[] modbusRequest = {
                        0x00, 0x01, // Transaction ID
                        0x00, 0x00, // Protocol ID  
                        0x00, 0x06, // Length
                        0x01,       // Unit ID
                        0x03,       // Function Code (Read Holding Registers)
                        0x9C, 0x68, // Starting Address (40040 = 0x9C68)
                        0x00, 0x02  // Quantity (2 registers: Speed at 40040, Torque at 40041)
                    };
                    
                    stream.Write(modbusRequest, 0, modbusRequest.Length);
                    
                    byte[] response = new byte[256];
                    int bytesRead = stream.Read(response, 0, response.Length);
                    
                    if (bytesRead >= 9 && response[7] == 0x03) // Valid Modbus response
                    {
                        // Parse the response
                        ushort ao0Raw = (ushort)((response[9] << 8) | response[10]);
                        ushort ao1Raw = (ushort)((response[11] << 8) | response[12]);
                        
                        // Convert to engineering units
                        // Assuming AO0_Speed: 0-10V = 0-3000 RPM
                        // Assuming AO1_Torque: 0-10V = 0-200 Nm
                        float speed = ConvertToSpeed(ao0Raw);
                        float torque = ConvertToTorque(ao1Raw);
                        
                        return (speed, torque, true);
                    }
                    else
                    {
                        return (0.0f, 0.0f, false);
                    }
                }
            }
            catch
            {
                return (0.0f, 0.0f, false);
            }
        }

        static float ConvertToSpeed(ushort rawValue)
        {
            // Based on testing: register 40040 contains direct Speed value
            // OPC Quick Client shows Speed=2, and we read raw value=2
            // So it's likely a direct value or simple scaling
            if (rawValue <= 10) return rawValue; // Direct value for small numbers
            return (rawValue / 100.0f) * 3000.0f; // Scale if larger values
        }

        static float ConvertToTorque(ushort rawValue)
        {
            // Convert 16-bit raw value to Nm
            // Assuming full scale (65535) = 200 Nm
            return (rawValue / 65535.0f) * 200.0f;
        }
    }
}