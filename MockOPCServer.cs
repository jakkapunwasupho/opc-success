using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace TestOPC
{
    // Mock OPC Server for testing purposes
    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789012")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MockOPCServer
    {
        private bool _connected = false;
        private MockOPCGroups _groups;

        public MockOPCServer()
        {
            _groups = new MockOPCGroups();
        }

        public void Connect(string serverName)
        {
            Console.WriteLine($"Mock: Connecting to {serverName}");
            _connected = true;
            Console.WriteLine("Mock: Connected successfully");
        }

        public void Disconnect()
        {
            Console.WriteLine("Mock: Disconnecting");
            _connected = false;
            Console.WriteLine("Mock: Disconnected");
        }

        public MockOPCGroups OPCGroups => _groups;
    }

    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789013")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MockOPCGroups
    {
        public MockOPCGroup Add(string groupName)
        {
            Console.WriteLine($"Mock: Creating group {groupName}");
            return new MockOPCGroup(groupName);
        }
    }

    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789014")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MockOPCGroup
    {
        private string _name;
        private MockOPCItems _items;
        public int UpdateRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsSubscribed { get; set; }

        public MockOPCGroup(string name)
        {
            _name = name;
            _items = new MockOPCItems();
        }

        public MockOPCItems OPCItems => _items;

        public void SyncRead(short source, int numItems, int[] handles, out object[] values, out object[] errors)
        {
            Console.WriteLine($"Mock: Reading {numItems} items synchronously");
            
            values = new object[numItems];
            errors = new object[numItems];
            
            // Generate mock data
            Random rand = new Random();
            for (int i = 0; i < numItems; i++)
            {
                values[i] = rand.Next(0, 1000); // Mock sensor values
                errors[i] = 0; // No errors
            }
            
            Console.WriteLine($"Mock: Generated values: [{string.Join(", ", values)}]");
        }
    }

    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789015")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MockOPCItems
    {
        public MockOPCItem AddItem(string itemName, int clientHandle)
        {
            Console.WriteLine($"Mock: Adding item {itemName} with handle {clientHandle}");
            return new MockOPCItem(itemName, clientHandle);
        }
    }

    [ComVisible(true)]
    [Guid("12345678-1234-1234-1234-123456789016")]
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class MockOPCItem
    {
        public string ItemID { get; }
        public int ClientHandle { get; }

        public MockOPCItem(string itemId, int clientHandle)
        {
            ItemID = itemId;
            ClientHandle = clientHandle;
        }
    }
}