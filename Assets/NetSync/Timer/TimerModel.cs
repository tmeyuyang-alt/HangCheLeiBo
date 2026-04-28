namespace GameServer
{
    public class TimerModel
    {
        public long Time;

        public System.Action<object> callback;

        public object param;

        public TimerModel(long Time)
        {
            this.Time = Time;
        }

        public void Run()
        {
            if(callback!=null)
            callback(param);
        }
    }
}
