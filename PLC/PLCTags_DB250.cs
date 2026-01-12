using S7.Net.Extensions;
using System;
using System.ComponentModel;
public class PLCTags_DB250 : INotifyPropertyChanged
{
    /// <summary>
    ///     ''' Raised when a property on this object has a new value.
    ///     ''' </summary>
    public event PropertyChangedEventHandler PropertyChanged;

    /// <summary>
    ///     ''' Raises this object's PropertyChanged event.
    ///     ''' </summary>
    ///     ''' <param name=propertyName>The property that has a new value.</param>
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
    private byte _L1L2_DB250_Protect_Read;
    [ParameterOrder(1)]
    public byte L1L2_DB250_Protect_Read
    {
        get
        {
            return _L1L2_DB250_Protect_Read;
        }
        set
        {
            if (_L1L2_DB250_Protect_Read != value)
            {
                _L1L2_DB250_Protect_Read = value;
                OnPropertyChanged("L1L2_DB250_Protect_Read");
            }
        }
    }

    //DBB1 
    private byte _L1L2_HBeat;
    [ParameterOrder(2)]
    public byte L1L2_HBeat
    {
        get
        {
            return _L1L2_HBeat;
        }
        set
        {
            if (_L1L2_HBeat != value)
            {
                _L1L2_HBeat = value;
                OnPropertyChanged("L1L2_HBeat");
            }
        }
    }

    //DBX 2.0 
    private bool _L1L2_LineRun;
    [ParameterOrder(3)]
    public bool L1L2_LineRun
    {
        get
        {
            return _L1L2_LineRun;
        }
        set
        {
            if (_L1L2_LineRun != value)
            {
                _L1L2_LineRun = value;
                OnPropertyChanged("L1L2_LineRun");
            }
        }
    }

    //DBX 2.1 
    private bool _L1L2_MotorTurned;
    [ParameterOrder(4)]
    public bool L1L2_MotorTurned
    {
        get
        {
            return _L1L2_MotorTurned;
        }
        set
        {
            if (_L1L2_MotorTurned != value)
            {
                _L1L2_MotorTurned = value;
                OnPropertyChanged("L1L2_MotorTurned");
            }
        }
    }

    //DBX 2.2 
    private bool _L1L2_ButtEnd;
    [ParameterOrder(5)]
    public bool L1L2_ButtEnd
    {
        get
        {
            return _L1L2_ButtEnd;
        }
        set
        {
            if (_L1L2_ButtEnd != value)
            {
                _L1L2_ButtEnd = value;
                OnPropertyChanged("L1L2_ButtEnd");
            }
        }
    }

    //DBX 2.3 
    private bool _L1L2_PipeCut;
    [ParameterOrder(6)]
    public bool L1L2_PipeCut
    {
        get
        {
            return _L1L2_PipeCut;
        }
        set
        {
            if (_L1L2_PipeCut != value)
            {
                _L1L2_PipeCut = value;
                OnPropertyChanged("L1L2_PipeCut");
            }
        }
    }

    //DBX 2.4 
    private bool _L1L2_Slit_Start;
    [ParameterOrder(7)]
    public bool L1L2_Slit_Start
    {
        get
        {
            return _L1L2_Slit_Start;
        }
        set
        {
            if (_L1L2_Slit_Start != value)
            {
                _L1L2_Slit_Start = value;
                OnPropertyChanged("L1L2_Slit_Start");
            }
        }
    }

    //DBX 2.5 
    private bool _L1L2_Slit_End;
    [ParameterOrder(8)]
    public bool L1L2_Slit_End
    {
        get
        {
            return _L1L2_Slit_End;
        }
        set
        {
            if (_L1L2_Slit_End != value)
            {
                _L1L2_Slit_End = value;
                OnPropertyChanged("L1L2_Slit_End");
            }
        }
    }

    //DBX 2.6 
    private bool _L1L2_New_Bundle_Start;
    [ParameterOrder(9)]
    public bool L1L2_New_Bundle_Start
    {
        get
        {
            return _L1L2_New_Bundle_Start;
        }
        set
        {
            if (_L1L2_New_Bundle_Start != value)
            {
                _L1L2_New_Bundle_Start = value;
                OnPropertyChanged("L1L2_New_Bundle_Start");
            }
        }
    }

    //DBX 2.7 
    private bool _L1L2_Bundle_End;
    [ParameterOrder(10)]
    public bool L1L2_Bundle_End
    {
        get
        {
            return _L1L2_Bundle_End;
        }
        set
        {
            if (_L1L2_Bundle_End != value)
            {
                _L1L2_Bundle_End = value;
                OnPropertyChanged("L1L2_Bundle_End");
            }
        }
    }

    //DBX 3.0 
    private bool _L1L2_Bundle_Pk;
    [ParameterOrder(11)]
    public bool L1L2_Bundle_Pk
    {
        get
        {
            return _L1L2_Bundle_Pk;
        }
        set
        {
            if (_L1L2_Bundle_Pk != value)
            {
                _L1L2_Bundle_Pk = value;
                OnPropertyChanged("L1L2_Bundle_Pk");
            }
        }
    }

    //DBX 3.1 
    private bool _L1L2_DB250_Spare_1;
    [ParameterOrder(12)]
    public bool L1L2_DB250_Spare_1
    {
        get
        {
            return _L1L2_DB250_Spare_1;
        }
        set
        {
            if (_L1L2_DB250_Spare_1 != value)
            {
                _L1L2_DB250_Spare_1 = value;
                OnPropertyChanged("L1L2_DB250_Spare_1");
            }
        }
    }

    //DBX 3.2 
    private bool _L1L2_SectionDone;
    [ParameterOrder(13)]
    public bool L1L2_SectionDone
    {
        get
        {
            return _L1L2_SectionDone;
        }
        set
        {
            if (_L1L2_SectionDone != value)
            {
                _L1L2_SectionDone = value;
                OnPropertyChanged("L1L2_SectionDone");
            }
        }
    }

    //DBX 3.3 
    private bool _L1L2_SectionReprint;
    [ParameterOrder(14)]
    public bool L1L2_SectionReprint
    {
        get
        {
            return _L1L2_SectionReprint;
        }
        set
        {
            if (_L1L2_SectionReprint != value)
            {
                _L1L2_SectionReprint = value;
                OnPropertyChanged("L1L2_SectionReprint");
            }
        }
    }

    //DBX 3.4 
    private bool _L1L2_PipeDone;
    [ParameterOrder(15)]
    public bool L1L2_PipeDone
    {
        get
        {
            return _L1L2_PipeDone;
        }
        set
        {
            if (_L1L2_PipeDone != value)
            {
                _L1L2_PipeDone = value;
                OnPropertyChanged("L1L2_PipeDone");
            }
        }
    }

    //DBX 3.5 
    private bool _L1L2_PipeReprint;
    [ParameterOrder(16)]
    public bool L1L2_PipeReprint
    {
        get
        {
            return _L1L2_PipeReprint;
        }
        set
        {
            if (_L1L2_PipeReprint != value)
            {
                _L1L2_PipeReprint = value;
                OnPropertyChanged("L1L2_PipeReprint");
            }
        }
    }

    //DBX 3.6 
    private bool _L1L2_SectionSensor;
    [ParameterOrder(17)]
    public bool L1L2_SectionSensor
    {
        get
        {
            return _L1L2_SectionSensor;
        }
        set
        {
            if (_L1L2_SectionSensor != value)
            {
                _L1L2_SectionSensor = value;
                OnPropertyChanged("L1L2_SectionSensor");
            }
        }
    }

    //DBX 3.7 
    private bool _L1L2_PipeSensor;
    [ParameterOrder(18)]
    public bool L1L2_PipeSensor
    {
        get
        {
            return _L1L2_PipeSensor;
        }
        set
        {
            if (_L1L2_PipeSensor != value)
            {
                _L1L2_PipeSensor = value;
                OnPropertyChanged("L1L2_PipeSensor");
            }
        }
    }

    //DBX 4.0 
    private bool _L1L2_Al_SectStopUp;
    [ParameterOrder(19)]
    public bool L1L2_Al_SectStopUp
    {
        get
        {
            return _L1L2_Al_SectStopUp;
        }
        set
        {
            if (_L1L2_Al_SectStopUp != value)
            {
                _L1L2_Al_SectStopUp = value;
                OnPropertyChanged("L1L2_Al_SectStopUp");
            }
        }
    }

    //DBX 4.1 
    private bool _L1L2_Al_SectStopDn;
    [ParameterOrder(20)]
    public bool L1L2_Al_SectStopDn
    {
        get
        {
            return _L1L2_Al_SectStopDn;
        }
        set
        {
            if (_L1L2_Al_SectStopDn != value)
            {
                _L1L2_Al_SectStopDn = value;
                OnPropertyChanged("L1L2_Al_SectStopDn");
            }
        }
    }

    //DBX 4.2 
    private bool _L1L2_Al_PipeStopUp;
    [ParameterOrder(21)]
    public bool L1L2_Al_PipeStopUp
    {
        get
        {
            return _L1L2_Al_PipeStopUp;
        }
        set
        {
            if (_L1L2_Al_PipeStopUp != value)
            {
                _L1L2_Al_PipeStopUp = value;
                OnPropertyChanged("L1L2_Al_PipeStopUp");
            }
        }
    }

    //DBX 4.3 
    private bool _L1L2_Al_PipeStopDn;
    [ParameterOrder(22)]
    public bool L1L2_Al_PipeStopDn
    {
        get
        {
            return _L1L2_Al_PipeStopDn;
        }
        set
        {
            if (_L1L2_Al_PipeStopDn != value)
            {
                _L1L2_Al_PipeStopDn = value;
                OnPropertyChanged("L1L2_Al_PipeStopDn");
            }
        }
    }

    //DBX 4.4 
    private bool _L1L2_AckReconOn;
    [ParameterOrder(23)]
    public bool L1L2_AckReconOn
    {
        get
        {
            return _L1L2_AckReconOn;
        }
        set
        {
            if (_L1L2_AckReconOn != value)
            {
                _L1L2_AckReconOn = value;
                OnPropertyChanged("L1L2_AckReconOn");
            }
        }
    }

   //DBX 4.5 
    private bool _L1L2_AckReconOff;
    [ParameterOrder(24)]
    public bool L1L2_AckReconOff
    {
        get
        {
            return _L1L2_AckReconOff;
        }
        set
        {
            if (_L1L2_AckReconOff != value)
            {
                _L1L2_AckReconOff = value;
                OnPropertyChanged("L1L2_AckReconOff");
            }
        }
    }

    //DBX 4.6 
    private bool _L1L2_AckReconSlit;
    [ParameterOrder(25)]
    public bool L1L2_AckReconSlit
    {
        get
        {
            return _L1L2_AckReconSlit;
        }
        set
        {
            if (_L1L2_AckReconSlit != value)
            {
                _L1L2_AckReconSlit = value;
                OnPropertyChanged("L1L2_AckReconSlit");
            }
        }
    }

    //DBX 4.7 
    private bool _L1L2_AckReconPacking;
    [ParameterOrder(26)]
    public bool L1L2_AckReconPacking
    {
        get
        {
            return _L1L2_AckReconPacking;
        }
        set
        {
            if (_L1L2_AckReconPacking != value)
            {
                _L1L2_AckReconPacking = value;
                OnPropertyChanged("L1L2_AckReconPacking");
            }
        }
    }

    //DBX 5.0 
    private bool _L1L2_PipeOk;
    [ParameterOrder(27)]
    public bool L1L2_PipeOk
    {
        get
        {
            return _L1L2_PipeOk;
        }
        set
        {
            if (_L1L2_PipeOk != value)
            {
                _L1L2_PipeOk = value;
                OnPropertyChanged("L1L2_PipeOk");
            }
        }
    }

    //DBX 5.1 
    private bool _L1L2_PipeNok;
    [ParameterOrder(28)]
    public bool L1L2_PipeNok
    {
        get
        {
            return _L1L2_PipeNok;
        }
        set
        {
            if (_L1L2_PipeNok != value)
            {
                _L1L2_PipeNok = value;
                OnPropertyChanged("L1L2_PipeNok");
            }
        }
    }

    //DBX 5.2 
    private bool _L1L2_PipeNDT;
    [ParameterOrder(29)]
    public bool L1L2_PipeNDT
    {
        get
        {
            return _L1L2_PipeNDT;
        }
        set
        {
            if (_L1L2_PipeNDT != value)
            {
                _L1L2_PipeNDT = value;
                OnPropertyChanged("L1L2_PipeNDT");
            }
        }
    }

    //DBX 5.3 
    private bool _L1L2_DB250_Spare_2;
    [ParameterOrder(30)]
    public bool L1L2_DB250_Spare_2
    {
        get
        {
            return _L1L2_DB250_Spare_2;
        }
        set
        {
            if (_L1L2_DB250_Spare_2 != value)
            {
                _L1L2_DB250_Spare_2 = value;
                OnPropertyChanged("L1L2_DB250_Spare_2");
            }
        }
    }

    //DBX 5.4 
    private bool _L1L2_DB250_Spare_3;
    [ParameterOrder(31)]
    public bool L1L2_DB250_Spare_3
    {
        get
        {
            return _L1L2_DB250_Spare_3;
        }
        set
        {
            if (_L1L2_DB250_Spare_3 != value)
            {
                _L1L2_DB250_Spare_3 = value;
                OnPropertyChanged("L1L2_DB250_Spare_3");
            }
        }
    }

    //DBX 5.5 
    private bool _L1L2_DB250_Spare_4;
    [ParameterOrder(32)]
    public bool L1L2_DB250_Spare_4
    {
        get
        {
            return _L1L2_DB250_Spare_4;
        }
        set
        {
            if (_L1L2_DB250_Spare_4 != value)
            {
                _L1L2_DB250_Spare_4 = value;
                OnPropertyChanged("L1L2_DB250_Spare_4");
            }
        }
    }

    //DBX 5.6 
    private bool _L1L2_DB250_Spare_5;
    [ParameterOrder(33)]
    public bool L1L2_DB250_Spare_5
    {
        get
        {
            return _L1L2_DB250_Spare_5;
        }
        set
        {
            if (_L1L2_DB250_Spare_5 != value)
            {
                _L1L2_DB250_Spare_5 = value;
                OnPropertyChanged("L1L2_DB250_Spare_5");
            }
        }
    }

    //DBX 5.7 
    private bool _L1L2_ShortCut;
    [ParameterOrder(34)]
    public bool L1L2_ShortCut
    {
        get
        {
            return _L1L2_ShortCut;
        }
        set
        {
            if (_L1L2_ShortCut != value)
            {
                _L1L2_ShortCut = value;
                OnPropertyChanged("L1L2_ShortCut");
            }
        }
    }

    //DBX 6.0 - NDT Bundle Done
    private bool _L1L2_NDTBundleDone;
    [ParameterOrder(35)]
    public bool L1L2_NDTBundleDone
    {
        get
        {
            return _L1L2_NDTBundleDone;
        }
        set
        {
            if (_L1L2_NDTBundleDone != value)
            {
                _L1L2_NDTBundleDone = value;
                OnPropertyChanged("L1L2_NDTBundleDone");
            }
        }
    }

    //DBX 6.1 - NDT Bundle Reprint
    private bool _L1L2_NDTBundleReprint;
    [ParameterOrder(36)]
    public bool L1L2_NDTBundleReprint
    {
        get
        {
            return _L1L2_NDTBundleReprint;
        }
        set
        {
            if (_L1L2_NDTBundleReprint != value)
            {
                _L1L2_NDTBundleReprint = value;
                OnPropertyChanged("L1L2_NDTBundleReprint");
            }
        }
    }

}
