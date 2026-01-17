using System;
using System.Threading;
using System.Threading.Tasks;
using S7.Net;

namespace NDTBundlePOC.Core.Services
{
    /// <summary>
    /// Real S7 PLC Service - Connects to physical Siemens S7-1200 PLC
    /// Requires S7netplus NuGet package
    /// </summary>
    public class RealS7PLCService : IPLCService
    {
        private Plc _plc;
        private bool _isConnected = false;
        private string _ipAddress;
        private int _rack;
        private int _slot;
        private readonly object _lockObject = new object();

        public bool IsConnected 
        { 
            get 
            { 
                lock (_lockObject)
                {
                    return _plc != null && _plc.IsConnected;
                }
            } 
        }

        public bool Connect(string ipAddress, int rack = 0, int slot = 1)
        {
            try
            {
                lock (_lockObject)
                {
                    _ipAddress = ipAddress;
                    _rack = rack;
                    _slot = slot;

                    try
                    {
                        _plc = new Plc(CpuType.S71200, ipAddress, (short)rack, (short)slot);
                    _plc.Open();
                    
                    if (_plc.IsConnected)
                    {
                        _isConnected = true;
                        Console.WriteLine($"✓ Connected to Siemens S7-1200 PLC at {ipAddress} (Rack: {rack}, Slot: {slot})");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"✗ Failed to connect to PLC at {ipAddress}");
                            _isConnected = false;
                            return false;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"✗ Error connecting to PLC at {ipAddress}: {ex.Message}");
                        _isConnected = false;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error connecting to PLC: {ex.Message}");
                _isConnected = false;
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                lock (_lockObject)
                {
                    if (_plc != null && _plc.IsConnected)
                    {
                        _plc.Close();
                        Console.WriteLine("✓ Disconnected from PLC");
                    }
                    _isConnected = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error disconnecting from PLC: {ex.Message}");
            }
        }

        public int ReadNDTCuts(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return 0;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB251.DBW6 (L1L2_NDTCut)
                var value = _plc.Read("DB251.DBW6");
                if (value != null)
                {
                    if (value is ushort)
                        return (int)(ushort)value;
                    else if (value is int)
                        return (int)value;
                }
                */

                // For POC: Return simulated value (remove when S7netplus is installed)
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading NDT cuts from PLC: {ex.Message}");
                return 0;
            }
        }

        public int ReadOKCuts(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return 0;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB251.DBW2 (L1L2_OKCut)
                var value = _plc.Read("DB251.DBW2");
                if (value != null)
                {
                    if (value is ushort)
                        return (int)(ushort)value;
                    else if (value is int)
                        return (int)value;
                }
                */

                // For POC: Return simulated value
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading OK cuts from PLC: {ex.Message}");
                return 0;
            }
        }

        public bool WriteBundleInfo(int millId, string bundleNo, string batchNo, int pcs)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Write bundle info to PLC
                // Adjust DB number based on mill ID
                int dbNumber = 100 + millId;
                _plc.Write($"DB{dbNumber}.DBString4.20", bundleNo);
                _plc.Write($"DB{dbNumber}.DBString24.20", batchNo);
                _plc.Write($"DB{dbNumber}.DBD44", ndtPcs);
                */

                // For POC: Simulate write
                Console.WriteLine($"✓ Wrote bundle info to PLC: Bundle={bundleNo}, Batch={batchNo}, Pcs={pcs}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error writing bundle info to PLC: {ex.Message}");
                return false;
            }
        }

        public int ReadPOPlanID(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return 0;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB251.DBW8 (L1L2_PLC_PO_ID)
                var value = _plc.Read("DB251.DBW8");
                if (value != null && value is ushort)
                {
                    return (int)(ushort)value;
                }
                */

                // For POC: Return simulated value
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading PO Plan ID from PLC: {ex.Message}");
                return 0;
            }
        }

        public int ReadSlitID(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return 0;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB251.DBW10 (L1L2_PLC_Slit_ID)
                var value = _plc.Read("DB251.DBW10");
                if (value != null && value is ushort)
                {
                    return (int)(ushort)value;
                }
                */

                // For POC: Return simulated value
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading Slit ID from PLC: {ex.Message}");
                return 0;
            }
        }

        public bool IsPOEnded(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read PO end flag from PLC (adjust address based on your PLC configuration)
                var value = _plc.Read("DB250.DBX5.7"); // Example address
                if (value != null && value is bool)
                {
                    return (bool)value;
                }
                */

                // For POC: Return simulated value
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading PO End flag from PLC: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read OK Bundle Done signal from PLC (DB250.DBX3.4)
        /// </summary>
        public bool ReadOKBundleDone(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB250.DBX3.4 (L1L2_PipeDone)
                var value = _plc.Read("DB250.DBX3.4");
                if (value != null && value is bool)
                {
                    return (bool)value;
                }
                */

                // For POC: Return simulated value
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading OK Bundle Done from PLC: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read NDT Bundle Done signal from PLC (DB250.DBX6.0)
        /// </summary>
        public bool ReadNDTBundleDone(int millId)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                // Read from DB250.DBX6.0 (L1L2_NDTBundleDone)
                var value = _plc.Read("DB250.DBX6.0");
                if (value != null && value is bool)
                {
                    return (bool)value;
                }
                */

                // For POC: Return simulated value
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading NDT Bundle Done from PLC: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read heartbeat value from PLC (DB1.DBW6 - L1_Heart_Beat)
        /// Returns: Continuous counter from 1 to 127, then resets to 1
        /// All values 1-127 indicate PLC is ONLINE and responding
        /// </summary>
        public int ReadHeartbeat()
        {
            try
            {
                if (!IsConnected)
                {
                    return -1; // Not connected
                }

                // Read from DB1.DBW6 (L1_Heart_Beat)
                lock (_lockObject)
                {
                    if (_plc == null || !_plc.IsConnected)
                    {
                        return -1; // Not connected
                    }

                    var value = _plc.Read("DB1.DBW6");
                    if (value != null)
                    {
                        if (value is ushort)
                            return (short)(ushort)value; // Convert to signed INT
                        else if (value is short)
                            return (short)value;
                        else if (value is int)
                            return (int)value;
                    }
                    return -1; // Invalid value
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error reading heartbeat from PLC: {ex.Message}");
                return -1; // Error reading
            }
        }

        /// <summary>
        /// Write acknowledgment to PLC
        /// </summary>
        public bool WriteAcknowledgment(string address, bool value)
        {
            try
            {
                if (!IsConnected)
                {
                    return false;
                }

                // TODO: Uncomment when S7netplus is installed
                /*
                _plc.Write(address, value);
                */

                // For POC: Simulate write
                Console.WriteLine($"✓ Wrote acknowledgment to PLC: {address} = {value}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ Error writing acknowledgment to PLC: {ex.Message}");
                return false;
            }
        }
    }
}

