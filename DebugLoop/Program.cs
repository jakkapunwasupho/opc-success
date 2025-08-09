using System;
using System.Net.Sockets;
using System.Threading;

namespace DebugADAMLoop
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Debug ADAM-5024 Data Reader ===");
            Console.WriteLine("Analyzing raw Modbus data to see actual values");
            Console.WriteLine("Press Ctrl+C to stop\n");

            StartDebugLoop();
        }

        static void StartDebugLoop()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            int readingCount = 0;

            Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════╗");
            Console.WriteLine("║                        DEBUG RAW MODBUS DATA                            ║");
            Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════╣");

            while (true)
            {
                try
                {
                    readingCount++;
                    DateTime now = DateTime.Now;
                    
                    var debugInfo = ReadADAMWithDebug(adamIP, adamPort);
                    
                    Console.WriteLine($"\n📊 Reading #{readingCount} - {now:HH:mm:ss.fff}");
                    Console.WriteLine("═══════════════════════════════════════════════════");
                    
                    if (debugInfo.success)
                    {
                        Console.WriteLine($"✅ Connection: SUCCESS");
                        Console.WriteLine($"📡 Response bytes: {debugInfo.responseLength}");
                        Console.WriteLine($"🔧 Raw response: {debugInfo.rawHex}");
                        Console.WriteLine($"📈 Raw AO0 (16-bit): {debugInfo.ao0Raw}");
                        Console.WriteLine($"📈 Raw AO1 (16-bit): {debugInfo.ao1Raw}");
                        Console.WriteLine($"🎯 AO0_Speed: {debugInfo.speed:F3} RPM");
                        Console.WriteLine($"⚡ AO1_Torque: {debugInfo.torque:F3} Nm");
                        
                        // ลองแปลงด้วยวิธีอื่น
                        float altSpeed1 = debugInfo.ao0Raw; // Direct value
                        float altSpeed2 = debugInfo.ao0Raw / 10.0f; // Divide by 10
                        float altSpeed3 = debugInfo.ao0Raw / 100.0f; // Divide by 100
                        float altSpeed4 = debugInfo.ao0Raw * 0.1f; // Scale by 0.1
                        
                        Console.WriteLine($"🔄 Alternative conversions:");
                        Console.WriteLine($"   Direct: {altSpeed1}");
                        Console.WriteLine($"   /10:    {altSpeed2}");
                        Console.WriteLine($"   /100:   {altSpeed3}");
                        Console.WriteLine($"   *0.1:   {altSpeed4}");
                        
                        if (debugInfo.ao0Raw > 0)
                        {
                            Console.WriteLine($"🎉 NON-ZERO VALUE DETECTED! Raw = {debugInfo.ao0Raw}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"❌ Connection: FAILED");
                        Console.WriteLine($"📡 Error: {debugInfo.error}");
                    }
                    
                    // รอ 2 วินาที (ช้าหน่อยเพื่อดู debug info)
                    Thread.Sleep(2000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"💥 Exception: {ex.Message}");
                    Thread.Sleep(2000);
                }
            }
        }

        static DebugInfo ReadADAMWithDebug(string ip, int port)
        {
            var debugInfo = new DebugInfo();
            
            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(ip, port);
                    
                    var stream = client.GetStream();
                    
                    // ลอง register addresses หลายแบบ รวม 400041 (0-based = 40040) ที่ user บอก
                    ushort[] startAddresses = { 0, 1, 40001, 40002, 30001, 30002, 40040, 40041 };
                    
                    foreach (ushort startAddr in startAddresses)
                    {
                        // Modbus TCP request
                        byte[] modbusRequest = {
                            0x00, 0x01, // Transaction ID
                            0x00, 0x00, // Protocol ID
                            0x00, 0x06, // Length
                            0x01,       // Unit ID
                            0x03,       // Function Code (Read Holding Registers)
                            (byte)(startAddr >> 8), (byte)(startAddr & 0xFF), // Starting Address
                            0x00, 0x02  // Quantity (2 registers)
                        };
                        
                        stream.Write(modbusRequest, 0, modbusRequest.Length);
                        
                        byte[] response = new byte[256];
                        int bytesRead = stream.Read(response, 0, response.Length);
                        
                        debugInfo.responseLength = bytesRead;
                        
                        if (bytesRead >= 9)
                        {
                            debugInfo.rawHex = BitConverter.ToString(response, 0, Math.Min(16, bytesRead));
                            
                            if (response[7] == 0x03) // Valid function code response
                            {
                                debugInfo.ao0Raw = (ushort)((response[9] << 8) | response[10]);
                                debugInfo.ao1Raw = (ushort)((response[11] << 8) | response[12]);
                                
                                // ลองหลายวิธีแปลง
                                debugInfo.speed = ConvertToSpeed(debugInfo.ao0Raw);
                                debugInfo.torque = ConvertToTorque(debugInfo.ao1Raw);
                                
                                debugInfo.success = true;
                                debugInfo.startAddress = startAddr;
                                
                                Console.WriteLine($"📍 Trying address {startAddr}: AO0={debugInfo.ao0Raw}, AO1={debugInfo.ao1Raw}");
                                
                                // ถ้าเจอค่าที่ไม่เป็น 0 ให้หยุดและใช้ address นี้
                                if (debugInfo.ao0Raw > 0 || debugInfo.ao1Raw > 0)
                                {
                                    Console.WriteLine($"🎯 Found non-zero values at address {startAddr}!");
                                    return debugInfo;
                                }
                            }
                        }
                        
                        Thread.Sleep(100); // รอระหว่าง request
                    }
                    
                    debugInfo.success = true;
                }
            }
            catch (Exception ex)
            {
                debugInfo.error = ex.Message;
                debugInfo.success = false;
            }
            
            return debugInfo;
        }

        static float ConvertToSpeed(ushort rawValue)
        {
            // ลองหลายวิธีแปลง
            if (rawValue == 0) return 0.0f;
            
            // Method 1: Direct scale (0-65535 = 0-3000 RPM)
            float method1 = (rawValue / 65535.0f) * 3000.0f;
            
            // Method 2: Assume 0-10000 = 0-3000 RPM
            float method2 = (rawValue / 10000.0f) * 3000.0f;
            
            // Method 3: Direct value
            float method3 = rawValue;
            
            // Method 4: Scaled by 0.1
            float method4 = rawValue * 0.1f;
            
            Console.WriteLine($"   Speed conversions: Direct={method3}, /65535*3000={method1:F1}, /10000*3000={method2:F1}, *0.1={method4:F1}");
            
            // ใช้วิธีที่เหมาะสมที่สุด
            if (rawValue <= 10000) return method2; // OPC typical range
            else return method1; // Full 16-bit range
        }

        static float ConvertToTorque(ushort rawValue)
        {
            if (rawValue == 0) return 0.0f;
            return rawValue * 0.1f; // Simple scaling
        }

        public class DebugInfo
        {
            public bool success = false;
            public string error = "";
            public int responseLength = 0;
            public string rawHex = "";
            public ushort ao0Raw = 0;
            public ushort ao1Raw = 0;
            public float speed = 0.0f;
            public float torque = 0.0f;
            public ushort startAddress = 0;
        }
    }
}