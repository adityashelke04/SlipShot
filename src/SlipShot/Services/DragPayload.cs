using System;
using System.Windows;

namespace SlipShot.Services;

// WPF's outer DataObject calls this for both managed and native OLE reads.
internal sealed class DragPayload(DataObject data, DragTrace trace) : IDataObject
{
    internal bool WasRead { get; private set; }

    public object GetData(string format, bool autoConvert)
    {
        object result = data.GetData(format, autoConvert);
        // A format query is not consumption. Once handed out, a path/stream may outlive the drag effect.
        if (result is not null) WasRead = true;
        trace.Record("data_read", format, available: result is not null);
        return result!;
    }
    public object GetData(string format) => GetData(format, true);
    public object GetData(Type format) => GetData(format.FullName!);
    public bool GetDataPresent(string format, bool autoConvert)
    {
        bool available = data.GetDataPresent(format, autoConvert);
        trace.Record("format_query", format, available);
        return available;
    }
    public bool GetDataPresent(string format) => GetDataPresent(format, true);
    public bool GetDataPresent(Type format) => GetDataPresent(format.FullName!);
    public string[] GetFormats(bool autoConvert) => data.GetFormats(autoConvert);
    public string[] GetFormats() => GetFormats(true);
    public void SetData(string format, object value, bool autoConvert) => data.SetData(format, value, autoConvert);
    public void SetData(string format, object value) => data.SetData(format, value);
    public void SetData(Type format, object value) => data.SetData(format, value);
    public void SetData(object value) => data.SetData(value);
}
