using System;
using System.Net.Sockets;
using System.Collections.Generic;

namespace ScanNonZero
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Scan for Non-Zero Registers ===");
            Console.WriteLine("Finding all registers with non-zero values to locate OPC Quick Client source\n");

            ScanForNonZeroValues();
        }

        static void ScanForNonZeroValues()
        {
            string adamIP = "localhost";
            int adamPort = 502;
            
            List<(ushort register, ushort value, string description)> nonZeroValues = new List<(ushort, ushort, string)>();

            try
            {
                using (var client = new TcpClient())
                {
                    client.ReceiveTimeout = 2000;
                    client.SendTimeout = 2000;
                    client.Connect(adamIP, adamPort);
                    
                    var stream = client.GetStream();
                    
                    Console.WriteLine("🔍 Scanning Holding Registers (Function Code 03):");
                    Console.WriteLine("═══════════════════════════════════════════════");
                    
                    // Scan ranges of Holding Registers
                    ScanRange(stream, 0x03, 0, 100, "Basic Holding Registers 0-99", nonZeroValues);
                    ScanRange(stream, 0x03, 40000, 100, "Holding Registers 40000-40099", nonZeroValues);
                    ScanRange(stream, 0x03, 30000, 100, "Holding Registers 30000-30099", nonZeroValues);
                    ScanRange(stream, 0x03, 10000, 100, "Holding Registers 10000-10099", nonZeroValues);
                    
                    Console.WriteLine("\n🔍 Scanning Input Registers (Function Code 04):");
                    Console.WriteLine("═══════════════════════════════════════════════");
                    
                    // Scan ranges of Input Registers
                    ScanRange(stream, 0x04, 0, 100, "Basic Input Registers 0-99", nonZeroValues);
                    ScanRange(stream, 0x04, 30000, 100, "Input Registers 30000-30099", nonZeroValues);
                    ScanRange(stream, 0x04, 40000, 100, "Input Registers 40000-40099", nonZeroValues);
                    
                    Console.WriteLine("\n📊 SUMMARY - All Non-Zero Values Found:");
                    Console.WriteLine("═══════════════════════════════════════════════");
                    
                    if (nonZeroValues.Count == 0)
                    {
                        Console.WriteLine("❌ No non-zero values found!");
                        Console.WriteLine("💡 This suggests OPC Quick Client might be reading from:");
                        Console.WriteLine("   - A different Modbus device/unit ID");
                        Console.WriteLine("   - Coils or Discrete Inputs (not registers)");
                        Console.WriteLine("   - A completely different protocol");
                    }
                    else
                    {
                        foreach (var (register, value, description) in nonZeroValues)
                        {
                            int basedRegister = register + 1;
                            Console.WriteLine($"🎯 {description}: Register {register} (1-based: {basedRegister}) = {value}");
                            
                            if (value == 2)
                            {
                                Console.WriteLine($"   ⭐ This might be the OPC Quick Client source (value = 2)!");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
        }
        
        static void ScanRange(NetworkStream stream, byte functionCode, ushort startRegister, ushort count, string description, List<(ushort, ushort, string)> nonZeroValues)
        {
            Console.WriteLine($"📡 {description}:");
            
            try
            {
                // Read multiple registers at once (up to 125 registers per request)
                int batchSize = Math.Min((int)count, 125);
                
                for (ushort offset = 0; offset < count; offset += (ushort)batchSize)
                {
                    ushort currentStart = (ushort)(startRegister + offset);
                    ushort currentCount = (ushort)Math.Min((int)batchSize, (int)(count - offset));
                    
                    var values = ReadRegisters(stream, functionCode, currentStart, currentCount);
                    
                    for (int i = 0; i < values.Length; i++)
                    {
                        if (values[i] != 0)
                        {
                            ushort register = (ushort)(currentStart + i);
                            nonZeroValues.Add((register, values[i], description));
                            Console.WriteLine($"   🔢 Register {register} = {values[i]} ⭐");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   ❌ Failed to scan {description}: {ex.Message}");
            }
        }
        
        static ushort[] ReadRegisters(NetworkStream stream, byte functionCode, ushort startRegister, ushort count)
        {
            try
            {
                byte[] request = {
                    0x00, 0x01, // Transaction ID
                    0x00, 0x00, // Protocol ID
                    0x00, 0x06, // Length
                    0x01,       // Unit ID
                    functionCode, // Function Code
                    (byte)(startRegister >> 8), (byte)(startRegister & 0xFF), // Starting register
                    (byte)(count >> 8), (byte)(count & 0xFF) // Quantity
                };
                
                stream.Write(request, 0, request.Length);
                
                byte[] response = new byte[512];
                int bytesRead = stream.Read(response, 0, response.Length);
                
                if (bytesRead >= 9 && response[7] == functionCode)
                {
                    int dataLength = response[8];
                    ushort[] values = new ushort[count];
                    
                    for (int i = 0; i < count && (i * 2 + 10) < bytesRead; i++)
                    {
                        values[i] = (ushort)((response[9 + i * 2] << 8) | response[10 + i * 2]);
                    }
                    
                    return values;
                }
                
                return new ushort[count]; // Return zeros if failed
            }
            catch
            {
                return new ushort[count]; // Return zeros if failed
            }
        }
    }
}