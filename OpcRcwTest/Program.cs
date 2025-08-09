using System;
using System.Runtime.InteropServices;
using OpcRcw.Da;
using OpcRcw.Comn;

namespace OpcRcwTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== OpcRcw.Da COM Interop Test ===");
            Console.WriteLine("Using OpcRcw.Da to read AO0_Speed from NI OPC Server\n");

            TestOpcRcwConnection();
        }

        static void TestOpcRcwConnection()
        {
            IOPCServer? opcServer = null;
            IOPCGroupStateMgt? group = null;
            IOPCItemMgt? itemMgt = null;

            try
            {
                Console.WriteLine("Step 1: Creating OPC Server COM object...");
                
                // สร้าง OPC Server instance โดยใช้ COM
                Type serverType = Type.GetTypeFromProgID("National Instruments.NIOPCServer.V5");
                if (serverType == null)
                {
                    Console.WriteLine("❌ Cannot create NI OPC Server instance");
                    Console.WriteLine("   Server might not be properly registered");
                    return;
                }

                object serverObject = Activator.CreateInstance(serverType);
                opcServer = (IOPCServer)serverObject;
                Console.WriteLine("✅ OPC Server COM object created");

                Console.WriteLine("\nStep 2: Getting OPC Server status...");
                opcServer.GetStatus(out OPCSERVERSTATUS serverStatus);
                Console.WriteLine($"   Server State: {serverStatus.dwServerState}");
                Console.WriteLine($"   Start Time: {DateTime.FromFileTime(serverStatus.ftStartTime)}");
                Console.WriteLine($"   Vendor Info: {serverStatus.szVendorInfo}");

                Console.WriteLine("\nStep 3: Creating OPC Group...");
                Guid riid = typeof(IOPCGroupStateMgt).GUID;
                opcServer.AddGroup(
                    "TestGroup",        // Group Name
                    1,                  // Active
                    1000,              // Update Rate (ms)
                    IntPtr.Zero,       // Client Handle
                    IntPtr.Zero,       // Time Bias
                    IntPtr.Zero,       // Percent Deadband
                    0,                 // LCID
                    out int serverHandle,
                    out int revisedUpdateRate,
                    ref riid,
                    out object groupObject
                );

                group = (IOPCGroupStateMgt)groupObject;
                itemMgt = (IOPCItemMgt)groupObject;
                Console.WriteLine($"✅ Group created with handle: {serverHandle}");
                Console.WriteLine($"   Revised update rate: {revisedUpdateRate}ms");

                Console.WriteLine("\nStep 4: Adding OPC Items...");
                
                // เตรียม item definitions
                OPCITEMDEF[] itemDefs = new OPCITEMDEF[2];
                
                // AO0_Speed item
                itemDefs[0].szAccessPath = "";
                itemDefs[0].szItemID = "ADAM5000TCP.ADAM-5024.AO0_Speed";
                itemDefs[0].bActive = 1;
                itemDefs[0].hClient = 1;
                itemDefs[0].dwBlobSize = 0;
                itemDefs[0].pBlob = IntPtr.Zero;
                itemDefs[0].vtRequestedDataType = (short)VarEnum.VT_R4; // Float

                // AO1_Torque item  
                itemDefs[1].szAccessPath = "";
                itemDefs[1].szItemID = "ADAM5000TCP.ADAM-5024.AO1_Torque";
                itemDefs[1].bActive = 1;
                itemDefs[1].hClient = 2;
                itemDefs[1].dwBlobSize = 0;
                itemDefs[1].pBlob = IntPtr.Zero;
                itemDefs[1].vtRequestedDataType = (short)VarEnum.VT_R4; // Float

                // Add items to group
                itemMgt.AddItems(
                    2,                  // Item count
                    itemDefs,          // Item definitions
                    out OPCITEMRESULT[] results,
                    out IntPtr errors
                );

                Console.WriteLine("✅ Items added to group:");
                for (int i = 0; i < 2; i++)
                {
                    int error = Marshal.ReadInt32(errors, i * sizeof(int));
                    if (error == 0) // S_OK
                    {
                        Console.WriteLine($"   {itemDefs[i].szItemID} - Server Handle: {results[i].hServer}");
                    }
                    else
                    {
                        Console.WriteLine($"   {itemDefs[i].szItemID} - Error: 0x{error:X8}");
                    }
                }

                Console.WriteLine("\nStep 5: Reading OPC Data...");
                
                // เตรียม handles สำหรับการอ่าน
                int[] serverHandles = new int[2];
                serverHandles[0] = results[0].hServer;
                serverHandles[1] = results[1].hServer;

                // สร้าง sync read interface
                IOPCSyncIO syncIO = (IOPCSyncIO)groupObject;
                
                // อ่านข้อมูลจริง 3 ครั้ง
                for (int readCount = 1; readCount <= 3; readCount++)
                {
                    syncIO.Read(
                        OPCDATASOURCE.OPC_DS_DEVICE,  // Read from device
                        2,                             // Item count
                        serverHandles,                 // Server handles
                        out IntPtr itemValues,
                        out IntPtr itemErrors
                    );

                    Console.WriteLine($"\n📊 Reading #{readCount} - {DateTime.Now:HH:mm:ss.fff}");
                    
                    for (int i = 0; i < 2; i++)
                    {
                        // อ่าน error code
                        int itemError = Marshal.ReadInt32(itemErrors, i * sizeof(int));
                        
                        if (itemError == 0) // S_OK
                        {
                            // อ่าน OPCITEMSTATE
                            IntPtr itemStatePtr = IntPtr.Add(itemValues, i * Marshal.SizeOf(typeof(OPCITEMSTATE)));
                            OPCITEMSTATE itemState = Marshal.PtrToStructure<OPCITEMSTATE>(itemStatePtr);
                            
                            // แปลง variant เป็นค่าจริง
                            object value = Marshal.GetObjectForNativeVariant(itemState.vDataValue);
                            
                            string itemName = i == 0 ? "AO0_Speed" : "AO1_Torque";
                            string unit = i == 0 ? "RPM" : "Nm";
                            
                            Console.WriteLine($"   🎯 {itemName}: {value} {unit} (Quality: 0x{itemState.wQuality:X4})");
                        }
                        else
                        {
                            string itemName = i == 0 ? "AO0_Speed" : "AO1_Torque";
                            Console.WriteLine($"   ❌ {itemName}: Error 0x{itemError:X8}");
                        }
                    }
                    
                    // พักระหว่างการอ่าน
                    if (readCount < 3)
                        System.Threading.Thread.Sleep(1000);
                }

                Console.WriteLine("\n🎉 Successfully read AO0_Speed from real OPC Server!");
            }
            catch (COMException comEx)
            {
                Console.WriteLine($"\n❌ COM Exception: {comEx.Message}");
                Console.WriteLine($"   HRESULT: 0x{comEx.HResult:X8}");
                
                if (comEx.HResult == unchecked((int)0x800401F0))
                {
                    Console.WriteLine("   🔍 CoInitialize not called - this is normal in .NET");
                }
                else if (comEx.HResult == unchecked((int)0x80040154))
                {
                    Console.WriteLine("   🔍 Class not registered - NI OPC Server may not be properly installed");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ Error: {ex.Message}");
                Console.WriteLine($"   Type: {ex.GetType().Name}");
            }
            finally
            {
                // Cleanup
                try
                {
                    if (itemMgt != null) Marshal.ReleaseComObject(itemMgt);
                    if (group != null) Marshal.ReleaseComObject(group);
                    if (opcServer != null) Marshal.ReleaseComObject(opcServer);
                    
                    Console.WriteLine("\nCleanup completed.");
                }
                catch (Exception cleanupEx)
                {
                    Console.WriteLine($"Cleanup error: {cleanupEx.Message}");
                }
            }
        }
    }
}