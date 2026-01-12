using S7.Net.Extensions;
using System;
using System.ComponentModel;
public class PLCTags_DB252 : INotifyPropertyChanged
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

      //DBD0 
    private double _L1L2_DB252_Protect_Read;
    [ParameterOrder(1)]
    public double L1L2_DB252_Protect_Read
    {
        get
        {
            return _L1L2_DB252_Protect_Read;
        }
        set
        {
            if (_L1L2_DB252_Protect_Read != value)
            {
                _L1L2_DB252_Protect_Read = value;
                OnPropertyChanged("L1L2_DB252_Protect_Read");
            }
        }
    }

   //DBD4 
    private double _L1L2_CutLength;
    [ParameterOrder(2)]
    public double L1L2_CutLength
    {
        get
        {
            return _L1L2_CutLength;
        }
        set
        {
            if (_L1L2_CutLength != value)
            {
                _L1L2_CutLength = value;
                OnPropertyChanged("L1L2_CutLength");
            }
        }
    }

   //DBD8 
    private double _L1L2_DB252_Spare_1;
    [ParameterOrder(3)]
    public double L1L2_DB252_Spare_1
    {
        get
        {
            return _L1L2_DB252_Spare_1;
        }
        set
        {
            if (_L1L2_DB252_Spare_1 != value)
            {
                _L1L2_DB252_Spare_1 = value;
                OnPropertyChanged("L1L2_DB252_Spare_1");
            }
        }
    }

   //DBD12 
    private double _L1L2_ShortLength;
    [ParameterOrder(4)]
    public double L1L2_ShortLength
    {
        get
        {
            return _L1L2_ShortLength;
        }
        set
        {
            if (_L1L2_ShortLength != value)
            {
                _L1L2_ShortLength = value;
                OnPropertyChanged("L1L2_ShortLength");
            }
        }
    }

   //DBD16 
    private double _L1L2_DB252_Spare_3;
    [ParameterOrder(5)]
    public double L1L2_DB252_Spare_3
    {
        get
        {
            return _L1L2_DB252_Spare_3;
        }
        set
        {
            if (_L1L2_DB252_Spare_3 != value)
            {
                _L1L2_DB252_Spare_3 = value;
                OnPropertyChanged("L1L2_DB252_Spare_3");
            }
        }
    }

   //DBD20 
    private double _L1L2_DB252_Spare_4;
    [ParameterOrder(6)]
    public double L1L2_DB252_Spare_4
    {
        get
        {
            return _L1L2_DB252_Spare_4;
        }
        set
        {
            if (_L1L2_DB252_Spare_4 != value)
            {
                _L1L2_DB252_Spare_4 = value;
                OnPropertyChanged("L1L2_DB252_Spare_4");
            }
        }
    }

   //DBD24 
    private double _L1L2_DB252_Spare_5;
    [ParameterOrder(7)]
    public double L1L2_DB252_Spare_5
    {
        get
        {
            return _L1L2_DB252_Spare_5;
        }
        set
        {
            if (_L1L2_DB252_Spare_5 != value)
            {
                _L1L2_DB252_Spare_5 = value;
                OnPropertyChanged("L1L2_DB252_Spare_5");
            }
        }
    }

   //DBD28 
    private double _L1L2_DB252_Spare_6;
    [ParameterOrder(8)]
    public double L1L2_DB252_Spare_6
    {
        get
        {
            return _L1L2_DB252_Spare_6;
        }
        set
        {
            if (_L1L2_DB252_Spare_6 != value)
            {
                _L1L2_DB252_Spare_6 = value;
                OnPropertyChanged("L1L2_DB252_Spare_6");
            }
        }
    }


}
