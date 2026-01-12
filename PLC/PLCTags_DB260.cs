using S7.Net.Extensions;
using System;
using System.ComponentModel;
public class PLCTags_DB260 : INotifyPropertyChanged
{
    /// <summary>
    ///     ''' Raised when a property on this object has a new value.
    ///     ''' </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     ''' Raises this object's PropertyChanged event.
    ///     ''' </summary>
    ///     ''' <param name="propertyName">The property that has a new value.</param>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChangedEventHandler handler = this.PropertyChanged;
        if (handler != null)
        {
            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }
    }
    //DBB0 
    private byte _L2L1_DB260_Protect_Read;
    [ParameterOrder(1)]
    public byte L2L1_DB260_Protect_Read
    {
        get
        {
            return _L2L1_DB260_Protect_Read;
        }
        set
        {
            if (_L2L1_DB260_Protect_Read != value)
            {
                _L2L1_DB260_Protect_Read = value;
                OnPropertyChanged("L2L1_DB260_Protect_Read");
            }
        }
    }

    //DBB1 
    private byte _L2L1_HBeat;
    [ParameterOrder(2)]
    public byte L2L1_HBeat
    {
        get
        {
            return _L2L1_HBeat;
        }
        set
        {
            if (_L2L1_HBeat != value)
            {
                _L2L1_HBeat = value;
                OnPropertyChanged("L2L1_HBeat");
            }
        }
    }

    //DBX 2.0 
    private bool _L2L1_MotorLocked;
    [ParameterOrder(3)]
    public bool L2L1_MotorLocked
    {
        get
        {
            return _L2L1_MotorLocked;
        }
        set
        {
            if (_L2L1_MotorLocked != value)
            {
                _L2L1_MotorLocked = value;
                OnPropertyChanged("L2L1_MotorLocked");
            }
        }
    }

    //DBX 2.1 
    private bool _L2L1_AckMotorTurned;
    [ParameterOrder(4)]
    public bool L2L1_AckMotorTurned
    {
        get
        {
            return _L2L1_AckMotorTurned;
        }
        set
        {
            if (_L2L1_AckMotorTurned != value)
            {
                _L2L1_AckMotorTurned = value;
                OnPropertyChanged("L2L1_AckMotorTurned");
            }
        }
    }

    //DBX 2.2 
    private bool _L2L1_AckButtEnd;
    [ParameterOrder(5)]
    public bool L2L1_AckButtEnd
    {
        get
        {
            return _L2L1_AckButtEnd;
        }
        set
        {
            if (_L2L1_AckButtEnd != value)
            {
                _L2L1_AckButtEnd = value;
                OnPropertyChanged("L2L1_AckButtEnd");
            }
        }
    }

    //DBX 2.3 
    private bool _L2L1_AckPipeCut;
    [ParameterOrder(6)]
    public bool L2L1_AckPipeCut
    {
        get
        {
            return _L2L1_AckPipeCut;
        }
        set
        {
            if (_L2L1_AckPipeCut != value)
            {
                _L2L1_AckPipeCut = value;
                OnPropertyChanged("L2L1_AckPipeCut");
            }
        }
    }

    //DBX 2.4 
    private bool _L2L1_AckSlit_Start;
    [ParameterOrder(7)]
    public bool L2L1_AckSlit_Start
    {
        get
        {
            return _L2L1_AckSlit_Start;
        }
        set
        {
            if (_L2L1_AckSlit_Start != value)
            {
                _L2L1_AckSlit_Start = value;
                OnPropertyChanged("L2L1_AckSlit_Start");
            }
        }
    }

    //DBX 2.5 
    private bool _L2L1_Ack_Slit_End;
    [ParameterOrder(8)]
    public bool L2L1_Ack_Slit_End
    {
        get
        {
            return _L2L1_Ack_Slit_End;
        }
        set
        {
            if (_L2L1_Ack_Slit_End != value)
            {
                _L2L1_Ack_Slit_End = value;
                OnPropertyChanged("L2L1_Ack_Slit_End");
            }
        }
    }

    //DBX 2.6 
    private bool _L2L1_AckNew_Bundle_Start;
    [ParameterOrder(9)]
    public bool L2L1_AckNew_Bundle_Start
    {
        get
        {
            return _L2L1_AckNew_Bundle_Start;
        }
        set
        {
            if (_L2L1_AckNew_Bundle_Start != value)
            {
                _L2L1_AckNew_Bundle_Start = value;
                OnPropertyChanged("L2L1_AckNew_Bundle_Start");
            }
        }
    }

    //DBX 2.7 
    private bool _L2L1_Ack_Bundle_End;
    [ParameterOrder(10)]
    public bool L2L1_Ack_Bundle_End
    {
        get
        {
            return _L2L1_Ack_Bundle_End;
        }
        set
        {
            if (_L2L1_Ack_Bundle_End != value)
            {
                _L2L1_Ack_Bundle_End = value;
                OnPropertyChanged("L2L1_Ack_Bundle_End");
            }
        }
    }

    //DBX 3.0 
    private bool _L2L1_Ack_Bundle_Pk;
    [ParameterOrder(11)]
    public bool L2L1_Ack_Bundle_Pk
    {
        get
        {
            return _L2L1_Ack_Bundle_Pk;
        }
        set
        {
            if (_L2L1_Ack_Bundle_Pk != value)
            {
                _L2L1_Ack_Bundle_Pk = value;
                OnPropertyChanged("L2L1_Ack_Bundle_Pk");
            }
        }
    }

    //DBX 3.1 
    private bool _L2L1_Slit_Scan_Avail;
    [ParameterOrder(12)]
    public bool L2L1_Slit_Scan_Avail
    {
        get
        {
            return _L2L1_Slit_Scan_Avail;
        }
        set
        {
            if (_L2L1_Slit_Scan_Avail != value)
            {
                _L2L1_Slit_Scan_Avail = value;
                OnPropertyChanged("L2L1_Slit_Scan_Avail");
            }
        }
    }

    //DBX 3.2 
    private bool _L2L1_AckSectionDone;
    [ParameterOrder(13)]
    public bool L2L1_AckSectionDone
    {
        get
        {
            return _L2L1_AckSectionDone;
        }
        set
        {
            if (_L2L1_AckSectionDone != value)
            {
                _L2L1_AckSectionDone = value;
                OnPropertyChanged("L2L1_AckSectionDone");
            }
        }
    }

    //DBX 3.3 
    private bool _L2L1_AckSectionReprint;
    [ParameterOrder(14)]
    public bool L2L1_AckSectionReprint
    {
        get
        {
            return _L2L1_AckSectionReprint;
        }
        set
        {
            if (_L2L1_AckSectionReprint != value)
            {
                _L2L1_AckSectionReprint = value;
                OnPropertyChanged("L2L1_AckSectionReprint");
            }
        }
    }

    //DBX 3.4 
    private bool _L2L1_AckPipeDone;
    [ParameterOrder(15)]
    public bool L2L1_AckPipeDone
    {
        get
        {
            return _L2L1_AckPipeDone;
        }
        set
        {
            if (_L2L1_AckPipeDone != value)
            {
                _L2L1_AckPipeDone = value;
                OnPropertyChanged("L2L1_AckPipeDone");
            }
        }
    }

    //DBX 3.5 
    private bool _L2L1_AckPipeReprint;
    [ParameterOrder(16)]
    public bool L2L1_AckPipeReprint
    {
        get
        {
            return _L2L1_AckPipeReprint;
        }
        set
        {
            if (_L2L1_AckPipeReprint != value)
            {
                _L2L1_AckPipeReprint = value;
                OnPropertyChanged("L2L1_AckPipeReprint");
            }
        }
    }

    //DBX 3.6 
    private bool _L2L1_PASEn;
    [ParameterOrder(17)]
    public bool L2L1_PASEn
    {
        get
        {
            return _L2L1_PASEn;
        }
        set
        {
            if (_L2L1_PASEn != value)
            {
                _L2L1_PASEn = value;
                OnPropertyChanged("L2L1_PASEn");
            }
        }
    }

    //DBX 3.7 
    private bool _L2L1_ReconOn;
    [ParameterOrder(18)]
    public bool L2L1_ReconOn
    {
        get
        {
            return _L2L1_ReconOn;
        }
        set
        {
            if (_L2L1_ReconOn != value)
            {
                _L2L1_ReconOn = value;
                OnPropertyChanged("L2L1_ReconOn");
            }
        }
    }

    //DBX 4.0 
    private bool _L2L1_ReconOff;
    [ParameterOrder(19)]
    public bool L2L1_ReconOff
    {
        get
        {
            return _L2L1_ReconOff;
        }
        set
        {
            if (_L2L1_ReconOff != value)
            {
                _L2L1_ReconOff = value;
                OnPropertyChanged("L2L1_ReconOff");
            }
        }
    }

    //DBX 4.1 
    private bool _L2L1_ReconSlit;
    [ParameterOrder(20)]
    public bool L2L1_ReconSlit
    {
        get
        {
            return _L2L1_ReconSlit;
        }
        set
        {
            if (_L2L1_ReconSlit != value)
            {
                _L2L1_ReconSlit = value;
                OnPropertyChanged("L2L1_ReconSlit");
            }
        }
    }

    //DBX 4.2 
    private bool _L2L1_ReconPacking;
    [ParameterOrder(21)]
    public bool L2L1_ReconPacking
    {
        get
        {
            return _L2L1_ReconPacking;
        }
        set
        {
            if (_L2L1_ReconPacking != value)
            {
                _L2L1_ReconPacking = value;
                OnPropertyChanged("L2L1_ReconPacking");
            }
        }
    }

    //DBX 4.3 
    private bool _L2L1_DB260_Spare_5;
    [ParameterOrder(22)]
    public bool L2L1_DB260_Spare_5
    {
        get
        {
            return _L2L1_DB260_Spare_5;
        }
        set
        {
            if (_L2L1_DB260_Spare_5 != value)
            {
                _L2L1_DB260_Spare_5 = value;
                OnPropertyChanged("L2L1_DB260_Spare_5");
            }
        }
    }

    //DBX 4.4 
    private bool _L2L1_AckShortCut;
    [ParameterOrder(23)]
    public bool L2L1_AckShortCut
    {
        get
        {
            return _L2L1_AckShortCut;
        }
        set
        {
            if (_L2L1_AckShortCut != value)
            {
                _L2L1_AckShortCut = value;
                OnPropertyChanged("L2L1_AckShortCut");
            }
        }
    }

    //DBX 4.5 
    private bool _L2L1_DB260_Spare_7;
    [ParameterOrder(24)]
    public bool L2L1_DB260_Spare_7
    {
        get
        {
            return _L2L1_DB260_Spare_7;
        }
        set
        {
            if (_L2L1_DB260_Spare_7 != value)
            {
                _L2L1_DB260_Spare_7 = value;
                OnPropertyChanged("L2L1_DB260_Spare_7");
            }
        }
    }

    //DBX 4.6 
    private bool _L2L1_DB260_Spare_8;
    [ParameterOrder(25)]
    public bool L2L1_DB260_Spare_8
    {
        get
        {
            return _L2L1_DB260_Spare_8;
        }
        set
        {
            if (_L2L1_DB260_Spare_8 != value)
            {
                _L2L1_DB260_Spare_8 = value;
                OnPropertyChanged("L2L1_DB260_Spare_8");
            }
        }
    }

    //DBX 4.7 
    private bool _L2L1_DB260_Spare_9;
    [ParameterOrder(26)]
    public bool L2L1_DB260_Spare_9
    {
        get
        {
            return _L2L1_DB260_Spare_9;
        }
        set
        {
            if (_L2L1_DB260_Spare_9 != value)
            {
                _L2L1_DB260_Spare_9 = value;
                OnPropertyChanged("L2L1_DB260_Spare_9");
            }
        }
    }

    //DBX 5.0 
    private bool _L2L1_DB260_Spare_10;
    [ParameterOrder(27)]
    public bool L2L1_DB260_Spare_10
    {
        get
        {
            return _L2L1_DB260_Spare_10;
        }
        set
        {
            if (_L2L1_DB260_Spare_10 != value)
            {
                _L2L1_DB260_Spare_10 = value;
                OnPropertyChanged("L2L1_DB260_Spare_10");
            }
        }
    }

    //DBX 5.1 
    private bool _L2L1_DB260_Spare_11;
    [ParameterOrder(28)]
    public bool L2L1_DB260_Spare_11
    {
        get
        {
            return _L2L1_DB260_Spare_11;
        }
        set
        {
            if (_L2L1_DB260_Spare_11 != value)
            {
                _L2L1_DB260_Spare_11 = value;
                OnPropertyChanged("L2L1_DB260_Spare_11");
            }
        }
    }

    //DBX 5.2 
    private bool _L2L1_DB260_Spare_12;
    [ParameterOrder(29)]
    public bool L2L1_DB260_Spare_12
    {
        get
        {
            return _L2L1_DB260_Spare_12;
        }
        set
        {
            if (_L2L1_DB260_Spare_12 != value)
            {
                _L2L1_DB260_Spare_12 = value;
                OnPropertyChanged("L2L1_DB260_Spare_12");
            }
        }
    }

    //DBX 5.3 
    private bool _L2L1_DB260_Spare_13;
    [ParameterOrder(30)]
    public bool L2L1_DB260_Spare_13
    {
        get
        {
            return _L2L1_DB260_Spare_13;
        }
        set
        {
            if (_L2L1_DB260_Spare_13 != value)
            {
                _L2L1_DB260_Spare_13 = value;
                OnPropertyChanged("L2L1_DB260_Spare_13");
            }
        }
    }

    //DBX 5.4 
    private bool _L2L1_DB260_Spare_14;
    [ParameterOrder(31)]
    public bool L2L1_DB260_Spare_14
    {
        get
        {
            return _L2L1_DB260_Spare_14;
        }
        set
        {
            if (_L2L1_DB260_Spare_14 != value)
            {
                _L2L1_DB260_Spare_14 = value;
                OnPropertyChanged("L2L1_DB260_Spare_14");
            }
        }
    }

    //DBX 5.5 
    private bool _L2L1_DB260_Spare_15;
    [ParameterOrder(32)]
    public bool L2L1_DB260_Spare_15
    {
        get
        {
            return _L2L1_DB260_Spare_15;
        }
        set
        {
            if (_L2L1_DB260_Spare_15 != value)
            {
                _L2L1_DB260_Spare_15 = value;
                OnPropertyChanged("L2L1_DB260_Spare_15");
            }
        }
    }

    //DBX 5.6 
    private bool _L2L1_DB260_Spare_16;
    [ParameterOrder(33)]
    public bool L2L1_DB260_Spare_16
    {
        get
        {
            return _L2L1_DB260_Spare_16;
        }
        set
        {
            if (_L2L1_DB260_Spare_16 != value)
            {
                _L2L1_DB260_Spare_16 = value;
                OnPropertyChanged("L2L1_DB260_Spare_16");
            }
        }
    }

    //DBX 5.7 
    private bool _L2L1_DB260_Spare_17;
    [ParameterOrder(34)]
    public bool L2L1_DB260_Spare_17
    {
        get
        {
            return _L2L1_DB260_Spare_17;
        }
        set
        {
            if (_L2L1_DB260_Spare_17 != value)
            {
                _L2L1_DB260_Spare_17 = value;
                OnPropertyChanged("L2L1_DB260_Spare_17");
            }
        }
    }

    //DBX 6.0 - Acknowledge NDT Bundle Done
    private bool _L2L1_AckNDTBundleDone;
    [ParameterOrder(35)]
    public bool L2L1_AckNDTBundleDone
    {
        get
        {
            return _L2L1_AckNDTBundleDone;
        }
        set
        {
            if (_L2L1_AckNDTBundleDone != value)
            {
                _L2L1_AckNDTBundleDone = value;
                OnPropertyChanged("L2L1_AckNDTBundleDone");
            }
        }
    }

    //DBX 6.1 - Acknowledge NDT Bundle Reprint
    private bool _L2L1_AckNDTBundleReprint;
    [ParameterOrder(36)]
    public bool L2L1_AckNDTBundleReprint
    {
        get
        {
            return _L2L1_AckNDTBundleReprint;
        }
        set
        {
            if (_L2L1_AckNDTBundleReprint != value)
            {
                _L2L1_AckNDTBundleReprint = value;
                OnPropertyChanged("L2L1_AckNDTBundleReprint");
            }
        }
    }

}
