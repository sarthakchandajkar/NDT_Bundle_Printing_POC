using S7.Net.Extensions;
using System;
using System.ComponentModel;
public class PLCTags_DB251 : INotifyPropertyChanged
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

    //DBW0 
    private ushort _L1L2_DB251_Protect_Read;
    [ParameterOrder(1)]
    public ushort L1L2_DB251_Protect_Read
    {
        get
        {
            return _L1L2_DB251_Protect_Read;
        }
        set
        {
            if (_L1L2_DB251_Protect_Read != value)
            {
                _L1L2_DB251_Protect_Read = value;
                OnPropertyChanged("L1L2_DB251_Protect_Read");
            }
        }
    }

    //DBW2 
    private ushort _L1L2_OKCut;
    [ParameterOrder(2)]
    public ushort L1L2_OKCut
    {
        get
        {
            return _L1L2_OKCut;
        }
        set
        {
            if (_L1L2_OKCut != value)
            {
                _L1L2_OKCut = value;
                OnPropertyChanged("L1L2_OKCut");
            }
        }
    }

    //DBW4 
    private ushort _L1L2_NOKCut;
    [ParameterOrder(3)]
    public ushort L1L2_NOKCut
    {
        get
        {
            return _L1L2_NOKCut;
        }
        set
        {
            if (_L1L2_NOKCut != value)
            {
                _L1L2_NOKCut = value;
                OnPropertyChanged("L1L2_NOKCut");
            }
        }
    }

    //DBW6 
    private ushort _L1L2_NDTCut;
    [ParameterOrder(4)]
    public ushort L1L2_NDTCut
    {
        get
        {
            return _L1L2_NDTCut;
        }
        set
        {
            if (_L1L2_NDTCut != value)
            {
                _L1L2_NDTCut = value;
                OnPropertyChanged("L1L2_NDTCut");
            }
        }
    }

    //DBW8 
    private ushort _L1L2_PLC_PO_ID;
    [ParameterOrder(5)]
    public ushort L1L2_PLC_PO_ID
    {
        get
        {
            return _L1L2_PLC_PO_ID;
        }
        set
        {
            if (_L1L2_PLC_PO_ID != value)
            {
                _L1L2_PLC_PO_ID = value;
                OnPropertyChanged("L1L2_PLC_PO_ID");
            }
        }
    }

    //DBW10 
    private ushort _L1L2_PLC_Slit_ID;
    [ParameterOrder(6)]
    public ushort L1L2_PLC_Slit_ID
    {
        get
        {
            return _L1L2_PLC_Slit_ID;
        }
        set
        {
            if (_L1L2_PLC_Slit_ID != value)
            {
                _L1L2_PLC_Slit_ID = value;
                OnPropertyChanged("L1L2_PLC_Slit_ID");
            }
        }
    }

    //DBW12 
    private ushort _L1L2_Bundle_PCs_Count;
    [ParameterOrder(7)]
    public ushort L1L2_Bundle_PCs_Count
    {
        get
        {
            return _L1L2_Bundle_PCs_Count;
        }
        set
        {
            if (_L1L2_Bundle_PCs_Count != value)
            {
                _L1L2_Bundle_PCs_Count = value;
                OnPropertyChanged("L1L2_Bundle_PCs_Count");
            }
        }
    }

    //DBW14 
    private ushort _L1L2_PLC_PO_ID_2;
    [ParameterOrder(8)]
    public ushort L1L2_PLC_PO_ID_2
    {
        get
        {
            return _L1L2_PLC_PO_ID_2;
        }
        set
        {
            if (_L1L2_PLC_PO_ID_2 != value)
            {
                _L1L2_PLC_PO_ID_2 = value;
                OnPropertyChanged("L1L2_PLC_PO_ID_2");
            }
        }
    }

    //DBW16 
    private ushort _L1L2_PLC_Slit_ID_2;
    [ParameterOrder(9)]
    public ushort L1L2_PLC_Slit_ID_2
    {
        get
        {
            return _L1L2_PLC_Slit_ID_2;
        }
        set
        {
            if (_L1L2_PLC_Slit_ID_2 != value)
            {
                _L1L2_PLC_Slit_ID_2 = value;
                OnPropertyChanged("L1L2_PLC_Slit_ID_2");
            }
        }
    }

    //DBW18 
    private ushort _L1L2_Bundle_PCs_Count_2;
    [ParameterOrder(10)]
    public ushort L1L2_Bundle_PCs_Count_2
    {
        get
        {
            return _L1L2_Bundle_PCs_Count_2;
        }
        set
        {
            if (_L1L2_Bundle_PCs_Count_2 != value)
            {
                _L1L2_Bundle_PCs_Count_2 = value;
                OnPropertyChanged("L1L2_Bundle_PCs_Count_2");
            }
        }
    }

    //DBW20 
    private ushort _L1L2_Slit_ID_SlitEnd;
    [ParameterOrder(11)]
    public ushort L1L2_Slit_ID_SlitEnd
    {
        get
        {
            return _L1L2_Slit_ID_SlitEnd;
        }
        set
        {
            if (_L1L2_Slit_ID_SlitEnd != value)
            {
                _L1L2_Slit_ID_SlitEnd = value;
                OnPropertyChanged("L1L2_Slit_ID_SlitEnd");
            }
        }
    }

    //DBW22 
    private ushort _L1L2_Slit_ID_BundleEnd;
    [ParameterOrder(12)]
    public ushort L1L2_Slit_ID_BundleEnd
    {
        get
        {
            return _L1L2_Slit_ID_BundleEnd;
        }
        set
        {
            if (_L1L2_Slit_ID_BundleEnd != value)
            {
                _L1L2_Slit_ID_BundleEnd = value;
                OnPropertyChanged("L1L2_Slit_ID_BundleEnd");
            }
        }
    }

    //DBW24 
    private ushort _L1L2_Slit_ID_BundlePk;
    [ParameterOrder(13)]
    public ushort L1L2_Slit_ID_BundlePk
    {
        get
        {
            return _L1L2_Slit_ID_BundlePk;
        }
        set
        {
            if (_L1L2_Slit_ID_BundlePk != value)
            {
                _L1L2_Slit_ID_BundlePk = value;
                OnPropertyChanged("L1L2_Slit_ID_BundlePk");
            }
        }
    }

    //DBW26 
    private ushort _L1L2_DB251_Spare1;
    [ParameterOrder(14)]
    public ushort L1L2_DB251_Spare1
    {
        get
        {
            return _L1L2_DB251_Spare1;
        }
        set
        {
            if (_L1L2_DB251_Spare1 != value)
            {
                _L1L2_DB251_Spare1 = value;
                OnPropertyChanged("L1L2_DB251_Spare1");
            }
        }
    }

    //DBW28 - NDT Bundle Pieces Count
    private ushort _L1L2_NDTBundle_PCs_Count;
    [ParameterOrder(15)]
    public ushort L1L2_NDTBundle_PCs_Count
    {
        get
        {
            return _L1L2_NDTBundle_PCs_Count;
        }
        set
        {
            if (_L1L2_NDTBundle_PCs_Count != value)
            {
                _L1L2_NDTBundle_PCs_Count = value;
                OnPropertyChanged("L1L2_NDTBundle_PCs_Count");
            }
        }
    }

    //DBW30 - NDT Bundle Number (encoded as ushort - use for reference)
    private ushort _L1L2_NDTBundle_No;
    [ParameterOrder(16)]
    public ushort L1L2_NDTBundle_No
    {
        get
        {
            return _L1L2_NDTBundle_No;
        }
        set
        {
            if (_L1L2_NDTBundle_No != value)
            {
                _L1L2_NDTBundle_No = value;
                OnPropertyChanged("L1L2_NDTBundle_No");
            }
        }
    }

}
