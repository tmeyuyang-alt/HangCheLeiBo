public class Point
{
    public enum DataType
    { 
        Real,
        Int,
        DInt,
        Word,
        Byte,
        DoubleWord,
        String,
    }
    public enum OperationType
    {
        None = 0,
        Read = 1,
        Write = 2,
        ReadWrite = 3
    }

    public DataType dataType;

    public OperationType operation;
}
